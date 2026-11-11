using System.ComponentModel.DataAnnotations;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>Who changed what and when. Holds no sensitive personal data.</summary>
public class AuditLog
{
    [Key]
    public long AuditId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}
