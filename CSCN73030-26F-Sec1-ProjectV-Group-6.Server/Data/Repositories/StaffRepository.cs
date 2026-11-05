using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>Filters for <see cref="StaffRepository.SearchAsync"/>. Null means "don't filter".</summary>
public record StaffSearchCriteria(
    string? Text,
    int? DepartmentId,
    int? ServiceId,
    string? Role,
    EmploymentStatus? Status,
    CredentialStatus? CredentialState,
    int Page,
    int PageSize);

public class StaffRepository(CmesDbContext db)
{
    /// <summary>Finds a staff member by staff number. With details, loads assignments and credentials too.</summary>
    public Task<Staff?> GetByNumberAsync(string staffNumber, bool withDetails = false)
    {
        IQueryable<Staff> query = db.Staff.Include(s => s.HomeDepartment);
        if (withDetails)
        {
            query = query
                .Include(s => s.Assignments).ThenInclude(a => a.Service).ThenInclude(svc => svc.Department)
                .Include(s => s.Credentials).ThenInclude(c => c.CredentialType)
                .AsSplitQuery();
        }
        return query.SingleOrDefaultAsync(s => s.StaffNumber == staffNumber);
    }

    public Task<bool> EmailInUseAsync(string normalisedEmail, int? exceptStaffId = null) =>
        db.Staff.AnyAsync(s => s.Email == normalisedEmail && s.StaffId != exceptStaffId);

    /// <summary>
    /// Takes the next value from the city-wide staff number sequence. SQL Server sequences never
    /// hand out the same value twice, even if the surrounding transaction rolls back.
    /// </summary>
    public async Task<string> NextStaffNumberAsync()
    {
        // ToListAsync (not SingleAsync) so EF doesn't wrap the SQL in a subquery:
        // NEXT VALUE FOR is not allowed inside a derived table.
        var values = await db.Database
            .SqlQueryRaw<long>($"SELECT NEXT VALUE FOR dbo.{CmesDbContext.StaffNumberSequence} AS [Value]")
            .ToListAsync();
        return StaffNumber.Format(values.Single());
    }

    public async Task<(List<Staff> Items, int Total)> SearchAsync(StaffSearchCriteria criteria, DateOnly today)
    {
        IQueryable<Staff> query = db.Staff;

        if (!string.IsNullOrWhiteSpace(criteria.Text))
        {
            var text = criteria.Text.Trim();
            query = query.Where(s =>
                s.StaffNumber.Contains(text) ||
                s.FirstName.Contains(text) ||
                s.LastName.Contains(text) ||
                (s.PreferredName != null && s.PreferredName.Contains(text)) ||
                s.Email.Contains(text) ||
                (s.FirstName + " " + s.LastName).Contains(text) ||
                (s.PreferredName + " " + s.LastName).Contains(text));
        }

        if (criteria.DepartmentId != null)
            query = query.Where(s => s.HomeDepartmentId == criteria.DepartmentId);

        if (criteria.Status != null)
            query = query.Where(s => s.EmploymentStatus == criteria.Status);

        if (criteria.ServiceId != null)
            query = query.Where(s => s.Assignments.Any(a =>
                a.ServiceId == criteria.ServiceId &&
                a.StartDate <= today && (a.EndDate == null || a.EndDate >= today)));

        if (!string.IsNullOrWhiteSpace(criteria.Role))
        {
            var role = criteria.Role.Trim();
            query = query.Where(s => s.Assignments.Any(a =>
                a.RoleTitle.Contains(role) &&
                a.StartDate <= today && (a.EndDate == null || a.EndDate >= today)));
        }

        if (criteria.CredentialState != null)
        {
            var inState = CredentialStatusRules.IsInState(criteria.CredentialState.Value, today);
            query = query.Where(s => db.Credentials.Where(inState).Any(c => c.StaffId == s.StaffId));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ThenBy(s => s.StaffNumber)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Include(s => s.HomeDepartment)
            .Include(s => s.Assignments).ThenInclude(a => a.Service)
            .Include(s => s.Credentials)
            .AsSplitQuery()
            .ToListAsync();

        return (items, total);
    }

    public void Add(Staff staff) => db.Staff.Add(staff);

    public Task<IDbContextTransaction> BeginTransactionAsync() => db.Database.BeginTransactionAsync();

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
