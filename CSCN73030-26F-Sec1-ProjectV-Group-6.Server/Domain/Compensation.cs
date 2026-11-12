/**
 * @file Compensation.cs
 * @brief Compensation entity (schema only in this release; never exposed).
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// Pay band and salary history. Nothing in this release reads or writes this table; it exists
/// so the schema is complete for the payroll work next sprint.
/// </summary>
/// @par Business rule 4
/// Salary must never appear in any staff or search DTO.
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

    /// <summary>Annual salary. Never exposed by the API.</summary>
    public decimal AnnualSalary { get; set; }

    /// <summary>First day this pay applies.</summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Last day this pay applies, or null if current.</summary>
    public DateOnly? EffectiveTo { get; set; }
}
