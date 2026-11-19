using System.ComponentModel.DataAnnotations;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>An event saved in the same transaction as the change it reports, then published to other departments.</summary>
public class OutboxEvent
{
    [Key]
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
