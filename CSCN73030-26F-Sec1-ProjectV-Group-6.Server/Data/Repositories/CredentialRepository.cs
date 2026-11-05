using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

public class CredentialRepository(CmesDbContext db)
{
    public async Task<(List<Credential> Items, int Total)> SearchAsync(
        CredentialStatus? state, int? credentialTypeId, int? departmentId, DateOnly today, int page, int pageSize)
    {
        IQueryable<Credential> query = db.Credentials;

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
        WithDetails(db.Credentials)
            .Where(c => c.Status != CredentialStatus.Suspended && c.ExpiryDate >= today && c.ExpiryDate <= until)
            .OrderBy(c => c.ExpiryDate).ThenBy(c => c.CredentialId)
            .AsNoTracking()
            .ToListAsync();

    public Task<Credential?> GetByIdAsync(int credentialId) =>
        WithDetails(db.Credentials).SingleOrDefaultAsync(c => c.CredentialId == credentialId);

    public Task<CredentialType?> GetTypeAsync(int credentialTypeId) =>
        db.CredentialTypes.SingleOrDefaultAsync(t => t.CredentialTypeId == credentialTypeId);

    public void Add(Credential credential) => db.Credentials.Add(credential);

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    private static IQueryable<Credential> WithDetails(IQueryable<Credential> query) =>
        query.Include(c => c.CredentialType).Include(c => c.Staff).ThenInclude(s => s.HomeDepartment);
}
