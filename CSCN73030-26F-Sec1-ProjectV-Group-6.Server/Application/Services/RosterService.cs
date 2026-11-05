using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Duty roster: shifts, on-duty lookups and per-service headcount.</summary>
public class RosterService(
    RosterRepository rosterRepository,
    StaffRepository staffRepository,
    DepartmentRepository departmentRepository,
    Clock clock)
{
    public const int DefaultWindowDays = 7;
    public const int MaxWindowDays = 62;
    public static readonly TimeSpan MaxShiftLength = TimeSpan.FromHours(24);

    /// <summary>Shifts overlapping [from, to). Defaults to the 7 days starting today (UTC).</summary>
    public async Task<List<RosterEntryDto>> ListAsync(int? serviceId, DateTime? from, DateTime? to)
    {
        var start = from != null ? Clock.AsUtc(from.Value) : clock.Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to != null ? Clock.AsUtc(to.Value) : start.AddDays(DefaultWindowDays);

        var errors = new ValidationErrors();
        errors.AddIf(start >= end, "to", "'to' must be after 'from'.");
        errors.AddIf(end - start > TimeSpan.FromDays(MaxWindowDays), "to", $"The window can be at most {MaxWindowDays} days.");
        await RequireServiceIfGiven(errors, serviceId);
        errors.ThrowIfAny();

        var entries = await rosterRepository.ListAsync(serviceId, start, end);
        return entries.Select(e => e.ToDto()).ToList();
    }

    public async Task<RosterEntryDto> CreateEntryAsync(CreateRosterEntryRequest request)
    {
        var errors = new ValidationErrors();
        errors.Required("staffNumber", request.StaffNumber, StaffNumber.Length);
        errors.Required("serviceId", request.ServiceId);
        errors.Required("shiftStart", request.ShiftStart);
        errors.Required("shiftEnd", request.ShiftEnd);
        errors.Required("shiftType", request.ShiftType);

        DateTime? start = request.ShiftStart is { } s ? Clock.AsUtc(s) : null;
        DateTime? end = request.ShiftEnd is { } e ? Clock.AsUtc(e) : null;
        if (start != null && end != null)
        {
            // Business rule 6: ShiftStart < ShiftEnd.
            errors.AddIf(start >= end, "shiftEnd", "ShiftEnd must be after ShiftStart.");
            errors.AddIf(end - start > MaxShiftLength, "shiftEnd", "A shift can be at most 24 hours long.");
        }

        Staff? staff = null;
        if (!string.IsNullOrWhiteSpace(request.StaffNumber))
        {
            staff = await staffRepository.GetByNumberAsync(request.StaffNumber.Trim());
            errors.AddIf(staff == null, "staffNumber", $"Unknown staff number '{request.StaffNumber}'.");
            errors.AddIf(staff?.EmploymentStatus == EmploymentStatus.Terminated, "staffNumber",
                "A terminated employee cannot be rostered.");
        }
        await RequireServiceIfGiven(errors, request.ServiceId, "serviceId");
        errors.ThrowIfAny();

        // Business rule 6: no overlapping shifts for the same person.
        var overlap = await rosterRepository.FindOverlapAsync(staff!.StaffId, start!.Value, end!.Value);
        if (overlap != null)
            throw new ConflictException(
                $"{staff.StaffNumber} is already rostered from {overlap.ShiftStart:u} to {overlap.ShiftEnd:u} " +
                $"(roster entry {overlap.RosterEntryId}), which overlaps this shift.");

        var entry = new RosterEntry
        {
            StaffId = staff.StaffId,
            ServiceId = request.ServiceId!.Value,
            ShiftStart = start.Value,
            ShiftEnd = end.Value,
            ShiftType = request.ShiftType!.Value,
            Status = request.Status ?? RosterStatus.Scheduled,
        };
        rosterRepository.Add(entry);
        await rosterRepository.SaveChangesAsync();

        return (await rosterRepository.GetByIdAsync(entry.RosterEntryId))!.ToDto();
    }

    public async Task DeleteEntryAsync(int rosterEntryId)
    {
        var entry = await rosterRepository.GetByIdAsync(rosterEntryId)
                    ?? throw new NotFoundException("Roster entry", rosterEntryId);
        rosterRepository.Remove(entry);
        await rosterRepository.SaveChangesAsync();
    }

    public async Task<List<ServiceHeadcountDto>> HeadcountAsync(int? serviceId)
    {
        var errors = new ValidationErrors();
        await RequireServiceIfGiven(errors, serviceId);
        errors.ThrowIfAny();

        var rows = await rosterRepository.HeadcountByServiceAsync(serviceId, clock.Today, clock.UtcNow);
        return rows
            .Select(r => new ServiceHeadcountDto(r.Service.ServiceId, r.Service.Name, r.Service.Department.Code, r.Active, r.OnDuty))
            .ToList();
    }

    /// <summary>Shifts in progress at <paramref name="at"/> (default: now).</summary>
    public async Task<List<RosterEntryDto>> OnDutyAsync(int? serviceId, DateTime? at)
    {
        var errors = new ValidationErrors();
        await RequireServiceIfGiven(errors, serviceId);
        errors.ThrowIfAny();

        var instant = at != null ? Clock.AsUtc(at.Value) : clock.UtcNow;
        var entries = await rosterRepository.OnDutyAsync(serviceId, instant);
        return entries.Select(e => e.ToDto()).ToList();
    }

    private async Task RequireServiceIfGiven(ValidationErrors errors, int? serviceId, string field = "service")
    {
        if (serviceId == null) return;
        var exists = await departmentRepository.GetServiceAsync(serviceId.Value) != null;
        errors.AddIf(!exists, field, $"Unknown service id {serviceId}.");
    }
}
