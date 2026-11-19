/**
 * @file Compensation.cs
 * @brief Compensation entity: an employee's pay band and salary over time.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// One rate in an employee's pay history. Rates never overlap: setting a new rate closes the
/// current one on the new rate's EffectiveFrom.
/// </summary>
/// @par Business rule 4
/// Salary never appears in any staff or search DTO; it is only returned by the payroll endpoints
/// (compensation and payroll headcount), which are restricted to the payroll role.
public class Compensation
{
    /// <summary>Database key.</summary>
    public int CompensationId { get; set; }

    /// <summary>Employee's key.</summary>
    public int StaffId { get; set; }

    /// <summary>Employee.</summary>
    public Staff Staff { get; set; } = null!;

    /// <summary>Pay band code.</summary>
    public string PayBand { get; set; } = "";

    /// <summary>Annual salary in dollars. Only returned by the payroll endpoints.</summary>
    public decimal AnnualSalary { get; set; }

    /// <summary>First day this pay applies.</summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>First day this pay no longer applies (exclusive), or null while it is the current rate.</summary>
    public DateOnly? EffectiveTo { get; set; }
}
