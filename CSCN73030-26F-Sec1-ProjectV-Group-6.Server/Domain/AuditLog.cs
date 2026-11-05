namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>Who changed what and when. Schema only in this release.</summary>
public class AuditLog
{
    public long AuditId { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string ActorId { get; set; } = "";
    public DateTime TimestampUtc { get; set; }
}
