/**
 * @file SecurityFlag.cs
 * @brief SecurityFlag entity (schema only in this release).
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// A flag synced from the Security service, e.g. UnderInvestigation. One row per flag type per
/// person. An Active flag makes the person ineligible for duty.
/// </summary>
public class SecurityFlag
{
    /// <summary>Database key.</summary>
    public int FlagId { get; set; }

    /// <summary>Flagged employee's key.</summary>
    public int StaffId { get; set; }

    /// <summary>Flagged employee.</summary>
    public Staff Staff { get; set; } = null!;

    /// <summary>Kind of flag, as defined by the Security service (letters, digits, '.', '-' or '_'; max 50).</summary>
    public string FlagType { get; set; } = "";

    /// <summary>Active (blocks duty) or Cleared.</summary>
    public SecurityFlagStatus Status { get; set; }

    /// <summary>When the Security service last changed the flag, UTC. Older updates are ignored.</summary>
    public DateTime UpdatedAt { get; set; }
}
