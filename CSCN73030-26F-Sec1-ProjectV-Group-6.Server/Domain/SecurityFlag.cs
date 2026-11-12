/**
 * @file SecurityFlag.cs
 * @brief SecurityFlag entity (schema only in this release).
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A flag synced from the Security service. Schema only in this release.</summary>
public class SecurityFlag
{
    /// <summary>Database key.</summary>
    public int FlagId { get; set; }

    /// <summary>Flagged employee's key.</summary>
    public int StaffId { get; set; }

    /// <summary>Flagged employee.</summary>
    public Staff Staff { get; set; } = null!;

    /// <summary>Kind of flag, as defined by the Security service.</summary>
    public string FlagType { get; set; } = "";

    /// <summary>Flag status, as defined by the Security service.</summary>
    public string Status { get; set; } = "";

    /// <summary>When the flag last changed, UTC.</summary>
    public DateTime UpdatedAt { get; set; }
}
