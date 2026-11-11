namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A person's pay. Kept apart from Staff so payroll reports never expose individual salaries.</summary>
public class Compensation
{
    public int CompensationId { get; set; }
    public int StaffId { get; set; }
    public decimal AnnualSalary { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public Staff Staff { get; set; } = null!;
}
