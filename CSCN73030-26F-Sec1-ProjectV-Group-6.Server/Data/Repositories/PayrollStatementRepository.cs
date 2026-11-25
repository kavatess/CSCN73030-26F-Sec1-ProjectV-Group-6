/**
 * @file PayrollStatementRepository.cs
 * @brief Database access for payroll statements sent to Budget.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

/// <summary>Payroll statements, by month.</summary>
public class PayrollStatementRepository
{
    private readonly CmesDbContext _db;

    /// <summary>Creates the repository.</summary>
    public PayrollStatementRepository(CmesDbContext db)
    {
        _db = db;
    }

    /// <summary>The statements for one month, with their departments, ordered by department code.</summary>
    public Task<List<PayrollStatement>> ListForPeriodAsync(string period) =>
        _db.PayrollStatements.Include(p => p.Department)
            .Where(p => p.Period == period)
            .OrderBy(p => p.Department.Code)
            .ToListAsync();

    /// <summary>Adds a statement to be inserted on the next save.</summary>
    public void Add(PayrollStatement statement) => _db.PayrollStatements.Add(statement);

    /// <summary>Saves pending changes to the database.</summary>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
