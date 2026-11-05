namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// Pay band and salary history. Business rule 4: salary is never exposed by any staff or
/// search DTO. Nothing in this release reads or writes this table; it exists so the schema
/// is complete for the payroll work next sprint.
/// </summary>
public class Compensation
{
    public int CompensationId { get; set; }
    public int StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public string PayBand { get; set; } = "";
    public decimal AnnualSalary { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}
