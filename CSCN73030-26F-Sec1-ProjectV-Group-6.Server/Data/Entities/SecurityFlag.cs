/**
 * @file SecurityFlag.cs
 * @brief HR-02 schema draft entity (SecurityFlag).
 *
 * Part of the HR-02 schema draft (PR #3). Not registered in Program.cs and not used at runtime: the service uses CmesDbContext and the Domain entities. Its own migration lives in Migrations/; every dotnet ef command names --context CmesDbContext.
 */

using System.ComponentModel.DataAnnotations;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A flag raised by Security against a person (for example a suspension).</summary>
public class SecurityFlag
{
    [Key]
    public int FlagId { get; set; }
    public int StaffId { get; set; }
    public string FlagType { get; set; } = string.Empty;
    public SecurityFlagStatus Status { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Staff Staff { get; set; } = null!;
}
