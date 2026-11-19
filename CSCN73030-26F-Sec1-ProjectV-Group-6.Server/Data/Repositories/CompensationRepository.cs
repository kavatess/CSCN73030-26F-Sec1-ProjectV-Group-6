/**
 * @file CompensationRepository.cs
 * @brief Database access for pay history and payroll totals.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>Payroll figures for one department, as computed in SQL.</summary>
/// <param name="DepartmentId">Department key.</param>
/// <param name="Headcount">Employees on payroll.</param>
/// <param name="FullTime">Of those, FullTime.</param>
/// <param name="PartTime">Of those, PartTime, Contract or Seasonal.</param>
/// <param name="WithCompensation">Of those, with a rate in force.</param>
/// <param name="TotalAnnualSalary">Sum of the rates in force.</param>
public record DepartmentPayroll(int DepartmentId, int Headcount, int FullTime, int PartTime, int WithCompensation, decimal TotalAnnualSalary);

/// <summary>EF Core queries and writes for <see cref="Compensation"/>.</summary>
public class CompensationRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the repository.</summary>
    /// <param name="db">Database context.</param>
    public CompensationRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>A staff member's pay history, newest first (tracked, so the current rate can be closed).</summary>
    /// <param name="staffId">Staff key.</param>
    public Task<List<Compensation>> ListForStaffAsync(int staffId) =>
        _db.Compensations.Include(c => c.Staff)
            .Where(c => c.StaffId == staffId)
            .OrderByDescending(c => c.EffectiveFrom)
            .ToListAsync();

    /// <summary>
    /// Per-department payroll figures on <paramref name="asOf"/>: employees hired by then and not
    /// terminated, and the rate in force for each (EffectiveFrom &lt;= asOf &lt; EffectiveTo).
    /// </summary>
    /// <param name="asOf">Payroll date.</param>
    public Task<List<DepartmentPayroll>> PayrollByDepartmentAsync(DateOnly asOf) =>
        _db.Staff
            .Where(s => s.EmploymentStatus != EmploymentStatus.Terminated && s.HireDate <= asOf)
            .Select(s => new
            {
                s.HomeDepartmentId,
                FullTime = s.EmploymentType == EmploymentType.FullTime,
                Salary = _db.Compensations
                    .Where(c => c.StaffId == s.StaffId && c.EffectiveFrom <= asOf && (c.EffectiveTo == null || c.EffectiveTo > asOf))
                    .Select(c => (decimal?)c.AnnualSalary)
                    .FirstOrDefault(),
            })
            .GroupBy(x => x.HomeDepartmentId)
            .Select(g => new DepartmentPayroll(
                g.Key,
                g.Count(),
                g.Count(x => x.FullTime),
                g.Count(x => !x.FullTime),
                g.Count(x => x.Salary != null),
                g.Sum(x => x.Salary ?? 0m)))
            .ToListAsync();

    /// <summary>Adds a new rate to be inserted on the next save.</summary>
    /// <param name="compensation">The new rate.</param>
    public void Add(Compensation compensation) => _db.Compensations.Add(compensation);

    /// <summary>Saves pending changes to the database.</summary>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
