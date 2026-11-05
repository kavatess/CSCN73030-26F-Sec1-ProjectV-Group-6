namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// Transactional outbox row for integration events (Staff.Onboarded, Staff.Terminated).
/// Schema only in this release; nothing publishes to Azure Service Bus yet.
/// </summary>
public class OutboxEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = "";
    public int SchemaVersion { get; set; }
    public string PayloadJson { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
