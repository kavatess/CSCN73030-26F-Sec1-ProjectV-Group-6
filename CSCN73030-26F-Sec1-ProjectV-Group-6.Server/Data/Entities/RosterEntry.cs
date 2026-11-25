/**
 * @file RosterEntry.cs
 * @brief HR-02 schema draft entity (RosterEntry).
 *
 * Part of the HR-02 schema draft (PR #3). Not registered in Program.cs and not used at runtime: the service uses CmesDbContext and the Domain entities. Its own migration lives in Migrations/; every dotnet ef command names --context CmesDbContext.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>One shift on the duty roster.</summary>
public class RosterEntry
{
    public int RosterEntryId { get; set; }
    public int StaffId { get; set; }
    public int ServiceId { get; set; }
    public DateTime ShiftStart { get; set; }
    public DateTime ShiftEnd { get; set; }

    public Staff Staff { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
