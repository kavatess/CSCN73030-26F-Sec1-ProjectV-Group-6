using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

public class RequisitionRepository(CmesDbContext db)
{
    public Task<Requisition?> GetByNumberAsync(string requisitionNumber) =>
        WithDetails().SingleOrDefaultAsync(r => r.RequisitionNumber == requisitionNumber.Trim().ToUpper());

    public Task<Requisition?> GetByIdempotencyKeyAsync(string idempotencyKey) =>
        WithDetails().SingleOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey);

    public Task<List<Requisition>> ListAsync(RequisitionType? type, RequisitionStatus? status, int? departmentId) =>
        WithDetails()
            .Where(r => type == null || r.Type == type)
            .Where(r => status == null || r.Status == status)
            .Where(r => departmentId == null || r.RequestingDeptId == departmentId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>Next REQ-0000000 number from the requisition sequence.</summary>
    public async Task<string> NextRequisitionNumberAsync()
    {
        // ToListAsync, not SingleAsync: NEXT VALUE FOR is not allowed inside the subquery EF would add.
        var values = await db.Database
            .SqlQueryRaw<long>($"SELECT NEXT VALUE FOR dbo.{CmesDbContext.RequisitionNumberSequence} AS [Value]")
            .ToListAsync();
        return $"REQ-{values.Single():D7}";
    }

    public void Add(Requisition requisition) => db.Requisitions.Add(requisition);

    /// <summary>Forgets a requisition whose insert failed, so the context can be used again.</summary>
    public void Detach(Requisition requisition) => db.Entry(requisition).State = EntityState.Detached;

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    private IQueryable<Requisition> WithDetails() =>
        db.Requisitions.Include(r => r.RequestingDepartment).Include(r => r.Staff);
}
