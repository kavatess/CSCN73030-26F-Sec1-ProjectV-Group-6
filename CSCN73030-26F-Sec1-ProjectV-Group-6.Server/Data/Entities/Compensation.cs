/**
 * @file Compensation.cs
 * @brief HR-02 schema draft entity (Compensation).
 *
 * Part of the HR-02 schema draft (PR #3). Not registered in Program.cs and not used at runtime: the service uses CmesDbContext and the Domain entities. Its own migration lives in Migrations/; every dotnet ef command names --context CmesDbContext.
 */

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
