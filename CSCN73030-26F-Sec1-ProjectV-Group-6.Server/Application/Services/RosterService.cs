/**
 * @file RosterService.cs
 * @brief Business logic for the duty roster.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Duty roster: shifts, on-duty lookups and per-service headcount.</summary>
public class RosterService
{
    private readonly RosterRepository _rosterRepository;
    private readonly StaffRepository _staffRepository;
    private readonly DepartmentRepository _departmentRepository;
    private readonly Clock _clock;

    /// <summary>Creates the RosterService. Dependencies are supplied by dependency injection.</summary>
    /// <param name="rosterRepository">Roster data access.</param>
    /// <param name="staffRepository">Staff data access.</param>
    /// <param name="departmentRepository">Department and service data access.</param>
    /// <param name="clock">Current UTC time.</param>
    public RosterService(RosterRepository rosterRepository, StaffRepository staffRepository, DepartmentRepository departmentRepository, Clock clock)
    {
        _rosterRepository = rosterRepository;
        _staffRepository = staffRepository;
        _departmentRepository = departmentRepository;
        _clock = clock;
    }

    /// <summary>Days shown by GET /rosters when no end is given.</summary>
    public const int DefaultWindowDays = 7;
    /// <summary>Longest window GET /rosters accepts.</summary>
    public const int MaxWindowDays = 62;
    /// <summary>Longest shift accepted.</summary>
    public static readonly TimeSpan MaxShiftLength = TimeSpan.FromHours(24);

    /// <summary>Shifts overlapping [from, to). Defaults to the 7 days starting today (UTC).</summary>
    /// <param name="serviceId">Service to show, or null for all.</param>
    /// <param name="from">Window start, or null for midnight today (UTC).</param>
    /// <param name="to">Window end, or null for from + 7 days.</param>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<List<RosterEntryDto>> ListAsync(int? serviceId, DateTime? from, DateTime? to)
    {
        var start = from != null ? Clock.AsUtc(from.Value) : _clock.Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to != null ? Clock.AsUtc(to.Value) : start.AddDays(DefaultWindowDays);

        var errors = new ValidationErrors();
        errors.AddIf(start >= end, "to", "'to' must be after 'from'.");
        errors.AddIf(end - start > TimeSpan.FromDays(MaxWindowDays), "to", $"The window can be at most {MaxWindowDays} days.");
        await RequireServiceIfGiven(errors, serviceId);
        errors.ThrowIfAny();

        var entries = await _rosterRepository.ListAsync(serviceId, start, end);
        return entries.Select(e => e.ToDto()).ToList();
    }

    /// <summary>Adds a shift after checking dates, staff, service and overlaps.</summary>
    /// <param name="request">The shift.</param>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    /// <exception cref="Errors.ConflictException">The person already has an overlapping shift (409).</exception>
    /// @par Business rule 6
    /// ShiftStart must be before ShiftEnd, and a person's non-cancelled shifts may not overlap.
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
            staff = await _staffRepository.GetByNumberAsync(request.StaffNumber.Trim());
            errors.AddIf(staff == null, "staffNumber", $"Unknown staff number '{request.StaffNumber}'.");
            errors.AddIf(staff?.EmploymentStatus == EmploymentStatus.Terminated, "staffNumber",
                "A terminated employee cannot be rostered.");
        }
        await RequireServiceIfGiven(errors, request.ServiceId, "serviceId");
        errors.ThrowIfAny();

        // Business rule 6: no overlapping shifts for the same person.
        var overlap = await _rosterRepository.FindOverlapAsync(staff!.StaffId, start!.Value, end!.Value);
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
        _rosterRepository.Add(entry);
        await _rosterRepository.SaveChangesAsync();

        return (await _rosterRepository.GetByIdAsync(entry.RosterEntryId))!.ToDto();
    }

    /// <summary>Deletes a shift.</summary>
    /// <param name="rosterEntryId">Shift to delete.</param>
    /// <exception cref="Errors.NotFoundException">The roster entry does not exist.</exception>
    public async Task DeleteEntryAsync(int rosterEntryId)
    {
        var entry = await _rosterRepository.GetByIdAsync(rosterEntryId)
                    ?? throw new NotFoundException("Roster entry", rosterEntryId);
        _rosterRepository.Remove(entry);
        await _rosterRepository.SaveChangesAsync();
    }

    /// <summary>Active and on-duty counts per service.</summary>
    /// <param name="serviceId">One service, or null for all.</param>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<List<ServiceHeadcountDto>> HeadcountAsync(int? serviceId)
    {
        var errors = new ValidationErrors();
        await RequireServiceIfGiven(errors, serviceId);
        errors.ThrowIfAny();

        var rows = await _rosterRepository.HeadcountByServiceAsync(serviceId, _clock.Today, _clock.UtcNow);
        return rows
            .Select(r => new ServiceHeadcountDto(r.Service.ServiceId, r.Service.Name, r.Service.Department.Code, r.Active, r.OnDuty))
            .ToList();
    }

    /// <summary>Shifts in progress at <paramref name="at"/> (default: now).</summary>
    /// <param name="serviceId">One service, or null for all.</param>
    /// <param name="at">Instant to check, or null for now.</param>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<List<RosterEntryDto>> OnDutyAsync(int? serviceId, DateTime? at)
    {
        var errors = new ValidationErrors();
        await RequireServiceIfGiven(errors, serviceId);
        errors.ThrowIfAny();

        var instant = at != null ? Clock.AsUtc(at.Value) : _clock.UtcNow;
        var entries = await _rosterRepository.OnDutyAsync(serviceId, instant);
        return entries.Select(e => e.ToDto()).ToList();
    }

    private async Task RequireServiceIfGiven(ValidationErrors errors, int? serviceId, string field = "service")
    {
        if (serviceId == null) return;
        var exists = await _departmentRepository.GetServiceAsync(serviceId.Value) != null;
        errors.AddIf(!exists, field, $"Unknown service id {serviceId}.");
    }
}
