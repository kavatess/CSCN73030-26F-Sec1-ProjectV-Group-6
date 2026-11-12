/**
 * @file CredentialRepository.cs
 * @brief Database access for credentials.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>EF Core queries and writes for <see cref="Credential"/>.</summary>
public class CredentialRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the CredentialRepository. Dependencies are supplied by dependency injection.</summary>
    /// <param name="db">Database context.</param>
    public CredentialRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>Filters, counts and pages credentials, soonest expiry first.</summary>
    /// <param name="state">Derived state to match, or null.</param>
    /// <param name="credentialTypeId">Type to match, or null.</param>
    /// <param name="departmentId">Holder's home department key, or null.</param>
    /// <param name="today">Today (UTC).</param>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Rows per page.</param>
    public async Task<(List<Credential> Items, int Total)> SearchAsync(
        CredentialStatus? state, int? credentialTypeId, int? departmentId, DateOnly today, int page, int pageSize)
    {
        IQueryable<Credential> query = _db.Credentials;

        if (state != null)
            query = query.Where(CredentialStatusRules.IsInState(state.Value, today));
        if (credentialTypeId != null)
            query = query.Where(c => c.CredentialTypeId == credentialTypeId);
        if (departmentId != null)
            query = query.Where(c => c.Staff.HomeDepartmentId == departmentId);

        var total = await query.CountAsync();
        var items = await WithDetails(query)
            .OrderBy(c => c.ExpiryDate).ThenBy(c => c.CredentialId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, total);
    }

    /// <summary>Non-suspended credentials expiring between today and <paramref name="until"/> (inclusive).</summary>
    public Task<List<Credential>> ExpiringAsync(DateOnly today, DateOnly until) =>
        WithDetails(_db.Credentials)
            .Where(c => c.Status != CredentialStatus.Suspended && c.ExpiryDate >= today && c.ExpiryDate <= until)
            .OrderBy(c => c.ExpiryDate).ThenBy(c => c.CredentialId)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>Finds a credential with its type and holder.</summary>
    /// <param name="credentialId">Credential id.</param>
    public Task<Credential?> GetByIdAsync(int credentialId) =>
        WithDetails(_db.Credentials).SingleOrDefaultAsync(c => c.CredentialId == credentialId);

    /// <summary>Finds a credential type by id.</summary>
    /// <param name="credentialTypeId">Credential type id.</param>
    public Task<CredentialType?> GetTypeAsync(int credentialTypeId) =>
        _db.CredentialTypes.SingleOrDefaultAsync(t => t.CredentialTypeId == credentialTypeId);

    /// <summary>Adds a new credential to be inserted on the next save.</summary>
    /// <param name="credential">The new credential.</param>
    public void Add(Credential credential) => _db.Credentials.Add(credential);

    /// <summary>Saves pending changes to the database.</summary>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    private static IQueryable<Credential> WithDetails(IQueryable<Credential> query) =>
        query.Include(c => c.CredentialType).Include(c => c.Staff).ThenInclude(s => s.HomeDepartment);
}
