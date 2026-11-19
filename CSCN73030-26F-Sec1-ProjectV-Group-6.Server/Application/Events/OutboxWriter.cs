/**
 * @file OutboxWriter.cs
 * @brief Queues integration events in the outbox table, in the caller's transaction.
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;

/// <summary>
/// Transactional outbox: services call <see cref="Enqueue"/> before saving their change, so the
/// event row and the business change are committed together. OutboxDispatcher publishes it later.
/// </summary>
public class OutboxWriter
{
    /// <summary>JSON settings for payloads: camelCase, enums as strings.</summary>
    public static readonly JsonSerializerOptions PayloadJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly OutboxRepository _outboxRepository;
    private readonly Clock _clock;

    /// <summary>Creates the writer.</summary>
    /// <param name="outboxRepository">Outbox data access.</param>
    /// <param name="clock">Current UTC time.</param>
    public OutboxWriter(OutboxRepository outboxRepository, Clock clock)
    {
        _outboxRepository = outboxRepository;
        _clock = clock;
    }

    /// <summary>Adds an event row; it is written by the caller's next SaveChangesAsync.</summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="eventType">Event name, from <see cref="IntegrationEventTypes"/>.</param>
    /// <param name="payload">Event payload.</param>
    /// <returns>The new event's id (also the Service Bus message id).</returns>
    public Guid Enqueue<T>(string eventType, T payload)
    {
        var outboxEvent = new OutboxEvent
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            SchemaVersion = IntegrationEventTypes.SchemaVersion,
            PayloadJson = JsonSerializer.Serialize(payload, PayloadJson),
            OccurredAtUtc = _clock.UtcNow,
        };
        _outboxRepository.Add(outboxEvent);
        return outboxEvent.EventId;
    }
}
