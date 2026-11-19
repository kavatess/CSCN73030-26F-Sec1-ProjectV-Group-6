/**
 * @file RequisitionRepository.cs
 * @brief Database access for requisitions.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>EF Core queries and writes for <see cref="Requisition"/>.</summary>
public class RequisitionRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the RequisitionRepository. Dependencies are supplied by dependency injection.</summary>
    /// <param name="db">Database context.</param>
    public RequisitionRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>Finds a requisition by number, ignoring case.</summary>
    /// <param name="requisitionNumber">REQ-0000000.</param>
    public Task<Requisition?> GetByNumberAsync(string requisitionNumber) =>
        WithDetails().SingleOrDefaultAsync(r => r.RequisitionNumber == requisitionNumber.Trim().ToUpper());

    /// <summary>Finds the requisition created with this Idempotency-Key, if any (rule 3).</summary>
    /// <param name="idempotencyKey">The header value.</param>
    public Task<Requisition?> GetByIdempotencyKeyAsync(string idempotencyKey) =>
        WithDetails().IgnoreQueryFilters().SingleOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey); // keys are unique city-wide

    /// <summary>Requisitions matching the optional filters, newest first.</summary>
    /// <param name="type">Type to match, or null.</param>
    /// <param name="status">Status to match, or null.</param>
    /// <param name="departmentId">Requesting department key, or null.</param>
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
        var values = await _db.Database
            .SqlQueryRaw<long>($"SELECT NEXT VALUE FOR dbo.{CmesDbContext.RequisitionNumberSequence} AS [Value]")
            .ToListAsync();
        return $"REQ-{values.Single():D7}";
    }

    /// <summary>True when the staff member already has a termination requisition that is not Completed, Rejected or Closed.</summary>
    /// <param name="staffId">Staff key.</param>
    public Task<bool> HasOpenTerminationAsync(int staffId) =>
        _db.Requisitions.AnyAsync(r => r.StaffId == staffId && r.Type == RequisitionType.Termination &&
                                       r.Status != RequisitionStatus.Completed &&
                                       r.Status != RequisitionStatus.Rejected &&
                                       r.Status != RequisitionStatus.Closed);

    /// <summary>Adds a new requisition to be inserted on the next save.</summary>
    /// <param name="requisition">The new requisition.</param>
    public void Add(Requisition requisition) => _db.Requisitions.Add(requisition);

    /// <summary>Forgets a requisition whose insert failed, so the context can be used again.</summary>
    public void Detach(Requisition requisition) => _db.Entry(requisition).State = EntityState.Detached;

    /// <summary>Saves pending changes to the database.</summary>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    private IQueryable<Requisition> WithDetails() =>
        _db.Requisitions.Include(r => r.RequestingDepartment).Include(r => r.Staff);
}
