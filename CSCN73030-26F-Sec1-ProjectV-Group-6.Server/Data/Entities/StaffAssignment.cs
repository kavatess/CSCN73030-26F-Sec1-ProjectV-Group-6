/**
 * @file StaffAssignment.cs
 * @brief HR-02 schema draft entity (StaffAssignment).
 *
 * Part of the HR-02 schema draft (PR #3). Not registered in Program.cs and not used at runtime: the service uses CmesDbContext and the Domain entities. Its own migration lives in Migrations/; every dotnet ef command names --context CmesDbContext.
 */

using System.ComponentModel.DataAnnotations;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>Which service a person works in, and for how long. EndDate is null while current.</summary>
public class StaffAssignment
{
    [Key]
    public int AssignmentId { get; set; }
    public int StaffId { get; set; }
    public int ServiceId { get; set; }
    public string RoleTitle { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Staff Staff { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
