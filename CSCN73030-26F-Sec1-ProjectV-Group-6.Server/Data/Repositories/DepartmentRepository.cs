/**
 * @file DepartmentRepository.cs
 * @brief Database access for departments, services and headcounts.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>EF Core queries for departments, services and department headcount.</summary>
public class DepartmentRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the DepartmentRepository. Dependencies are supplied by dependency injection.</summary>
    /// <param name="db">Database context.</param>
    public DepartmentRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>Every department with its services, ordered by code.</summary>
    public Task<List<Department>> ListWithServicesAsync() =>
        _db.Departments.Include(d => d.Services).OrderBy(d => d.Code).AsNoTracking().ToListAsync();

    /// <summary>Finds a department by code, ignoring case and spaces.</summary>
    /// <param name="code">Department code, e.g. "ems".</param>
    public Task<Department?> GetByCodeAsync(string code) =>
        _db.Departments.SingleOrDefaultAsync(d => d.Code == code.Trim().ToUpper());

    /// <summary>A department's services, ordered by name.</summary>
    /// <param name="departmentId">Department key.</param>
    public Task<List<Service>> ListServicesAsync(int departmentId) =>
        _db.Services.Include(s => s.Department)
            .Where(s => s.DepartmentId == departmentId)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>Finds a service (with its department) by id.</summary>
    /// <param name="serviceId">Service id.</param>
    public Task<Service?> GetServiceAsync(int serviceId) =>
        _db.Services.Include(s => s.Department).SingleOrDefaultAsync(s => s.ServiceId == serviceId);

    /// <summary>Active + Probation staff per home department.</summary>
    public Task<Dictionary<int, int>> CountActiveStaffByDepartmentAsync() =>
        _db.Staff
            .Where(s => s.EmploymentStatus == EmploymentStatus.Active || s.EmploymentStatus == EmploymentStatus.Probation)
            .GroupBy(s => s.HomeDepartmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    /// <summary>Distinct staff on a non-cancelled shift at <paramref name="at"/>, per department of the service.</summary>
    public Task<Dictionary<int, int>> CountOnDutyByDepartmentAsync(DateTime at) =>
        _db.RosterEntries
            .Where(r => r.Status != RosterStatus.Cancelled && r.ShiftStart <= at && at < r.ShiftEnd)
            .Select(r => new { r.Service.DepartmentId, r.StaffId })
            .Distinct()
            .GroupBy(x => x.DepartmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    /// <summary>Unfilled positions on Approved or Filling hire requisitions, per requesting department.</summary>
    public Task<Dictionary<int, int>> CountVacanciesByDepartmentAsync() =>
        _db.Requisitions
            .Where(r => r.Type == RequisitionType.Hire &&
                        (r.Status == RequisitionStatus.Approved || r.Status == RequisitionStatus.Filling))
            .GroupBy(r => r.RequestingDeptId)
            .Select(g => new { g.Key, Open = g.Sum(r => r.Positions - r.PositionsFilled) })
            .ToDictionaryAsync(x => x.Key, x => x.Open);
}
