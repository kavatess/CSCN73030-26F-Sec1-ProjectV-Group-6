/**
 * @file AuditLog.cs
 * @brief AuditLog entity (schema only in this release).
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>Who changed what and when. Schema only in this release.</summary>
public class AuditLog
{
    /// <summary>Database key.</summary>
    public long AuditId { get; set; }

    /// <summary>Kind of record changed, e.g. "Staff".</summary>
    public string EntityType { get; set; } = "";

    /// <summary>Public id of the record changed, e.g. a staff number.</summary>
    public string EntityId { get; set; } = "";

    /// <summary>What was done, e.g. "Created".</summary>
    public string Action { get; set; } = "";

    /// <summary>Who did it.</summary>
    public string ActorId { get; set; } = "";

    /// <summary>When it happened, UTC.</summary>
    public DateTime TimestampUtc { get; set; }
}
