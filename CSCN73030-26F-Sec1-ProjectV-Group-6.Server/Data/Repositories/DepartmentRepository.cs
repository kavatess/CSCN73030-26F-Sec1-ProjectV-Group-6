using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

public class DepartmentRepository(CmesDbContext db)
{
    public Task<List<Department>> ListWithServicesAsync() =>
        db.Departments.Include(d => d.Services).OrderBy(d => d.Code).AsNoTracking().ToListAsync();

    public Task<Department?> GetByCodeAsync(string code) =>
        db.Departments.SingleOrDefaultAsync(d => d.Code == code.Trim().ToUpper());

    public Task<List<Service>> ListServicesAsync(int departmentId) =>
        db.Services.Include(s => s.Department)
            .Where(s => s.DepartmentId == departmentId)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync();

    public Task<Service?> GetServiceAsync(int serviceId) =>
        db.Services.Include(s => s.Department).SingleOrDefaultAsync(s => s.ServiceId == serviceId);

    /// <summary>Active + Probation staff per home department.</summary>
    public Task<Dictionary<int, int>> CountActiveStaffByDepartmentAsync() =>
        db.Staff
            .Where(s => s.EmploymentStatus == EmploymentStatus.Active || s.EmploymentStatus == EmploymentStatus.Probation)
            .GroupBy(s => s.HomeDepartmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    /// <summary>Distinct staff on a non-cancelled shift at <paramref name="at"/>, per department of the service.</summary>
    public Task<Dictionary<int, int>> CountOnDutyByDepartmentAsync(DateTime at) =>
        db.RosterEntries
            .Where(r => r.Status != RosterStatus.Cancelled && r.ShiftStart <= at && at < r.ShiftEnd)
            .Select(r => new { r.Service.DepartmentId, r.StaffId })
            .Distinct()
            .GroupBy(x => x.DepartmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    /// <summary>Unfilled positions on Approved or Filling hire requisitions, per requesting department.</summary>
    public Task<Dictionary<int, int>> CountVacanciesByDepartmentAsync() =>
        db.Requisitions
            .Where(r => r.Type == RequisitionType.Hire &&
                        (r.Status == RequisitionStatus.Approved || r.Status == RequisitionStatus.Filling))
            .GroupBy(r => r.RequestingDeptId)
            .Select(g => new { g.Key, Open = g.Sum(r => r.Positions - r.PositionsFilled) })
            .ToDictionaryAsync(x => x.Key, x => x.Open);
}
