using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

public class RosterRepository(CmesDbContext db)
{
    /// <summary>Entries that overlap the window [from, to), optionally for one service.</summary>
    public Task<List<RosterEntry>> ListAsync(int? serviceId, DateTime from, DateTime to) =>
        WithDetails()
            .Where(r => serviceId == null || r.ServiceId == serviceId)
            .Where(r => r.ShiftStart < to && from < r.ShiftEnd)
            .OrderBy(r => r.ShiftStart).ThenBy(r => r.Staff.LastName)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>Non-cancelled entries whose shift covers the instant <paramref name="at"/>.</summary>
    public Task<List<RosterEntry>> OnDutyAsync(int? serviceId, DateTime at) =>
        WithDetails()
            .Where(r => serviceId == null || r.ServiceId == serviceId)
            .Where(r => r.Status != RosterStatus.Cancelled && r.ShiftStart <= at && at < r.ShiftEnd)
            .OrderBy(r => r.Service.Name).ThenBy(r => r.Staff.LastName)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>
    /// Business rule 6: does this staff member already have a non-cancelled shift that overlaps
    /// [start, end)? Back-to-back shifts (one ends exactly when the next starts) do not overlap.
    /// </summary>
    public Task<RosterEntry?> FindOverlapAsync(int staffId, DateTime start, DateTime end) =>
        db.RosterEntries
            .Where(r => r.StaffId == staffId && r.Status != RosterStatus.Cancelled)
            .Where(r => r.ShiftStart < end && start < r.ShiftEnd)
            .OrderBy(r => r.ShiftStart)
            .FirstOrDefaultAsync();

    public Task<RosterEntry?> GetByIdAsync(int rosterEntryId) =>
        WithDetails().SingleOrDefaultAsync(r => r.RosterEntryId == rosterEntryId);

    /// <summary>Active and on-duty counts per service (all services, or just one).</summary>
    public async Task<List<(Service Service, int Active, int OnDuty)>> HeadcountByServiceAsync(
        int? serviceId, DateOnly today, DateTime now)
    {
        var services = await db.Services.Include(s => s.Department)
            .Where(s => serviceId == null || s.ServiceId == serviceId)
            .OrderBy(s => s.Department.Code).ThenBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync();

        var active = await db.StaffAssignments
            .Where(a => serviceId == null || a.ServiceId == serviceId)
            .Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
            .Where(a => a.Staff.EmploymentStatus == EmploymentStatus.Active ||
                        a.Staff.EmploymentStatus == EmploymentStatus.Probation)
            .Select(a => new { a.ServiceId, a.StaffId })
            .Distinct()
            .GroupBy(x => x.ServiceId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var onDuty = await db.RosterEntries
            .Where(r => serviceId == null || r.ServiceId == serviceId)
            .Where(r => r.Status != RosterStatus.Cancelled && r.ShiftStart <= now && now < r.ShiftEnd)
            .Select(r => new { r.ServiceId, r.StaffId })
            .Distinct()
            .GroupBy(x => x.ServiceId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return services
            .Select(s => (s, active.GetValueOrDefault(s.ServiceId), onDuty.GetValueOrDefault(s.ServiceId)))
            .ToList();
    }

    public void Add(RosterEntry entry) => db.RosterEntries.Add(entry);

    public void Remove(RosterEntry entry) => db.RosterEntries.Remove(entry);

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    private IQueryable<RosterEntry> WithDetails() =>
        db.RosterEntries.Include(r => r.Staff).Include(r => r.Service);
}
