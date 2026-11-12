/**
 * @file LoggingEventPublisher.cs
 * @brief Development stand-in for Service Bus: writes each event to the log.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Messaging;

/// <summary>Used when no Service Bus is configured: logs the event and treats it as published.</summary>
public class LoggingEventPublisher : IEventPublisher
{
    private readonly ILogger<LoggingEventPublisher> _logger;

    /// <summary>Creates the publisher.</summary>
    /// <param name="logger">Logger.</param>
    public LoggingEventPublisher(ILogger<LoggingEventPublisher> logger)
    {
        _logger = logger;
    }

    /// <summary>Logs the event.</summary>
    /// <param name="outboxEvent">The event row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task PublishAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Integration event {EventType} {EventId} (no Service Bus configured): {Payload}",
            outboxEvent.EventType, outboxEvent.EventId, outboxEvent.PayloadJson);
        return Task.CompletedTask;
    }
}
