/**
 * @file IEventPublisher.cs
 * @brief Sends one outbox event to the message broker.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;

/// <summary>Delivers an outbox event to other services. Throws if delivery fails, so the event is retried.</summary>
public interface IEventPublisher
{
    /// <summary>Publishes one event.</summary>
    /// <param name="outboxEvent">The event row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken);
}
