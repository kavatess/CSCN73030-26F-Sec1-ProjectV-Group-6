/**
 * @file OutboxDispatcher.cs
 * @brief Publishes pending outbox events, oldest first, and marks them published.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;

/// <summary>
/// One pass over the outbox: publishes pending events in order and stamps PublishedAtUtc.
/// Stops at the first failure so events are never delivered out of order; the failed event and
/// everything after it are retried on the next pass (delivery is at-least-once).
/// </summary>
public class OutboxDispatcher
{
    /// <summary>Most events published in one pass.</summary>
    public const int BatchSize = 50;

    private readonly OutboxRepository _outboxRepository;
    private readonly IEventPublisher _publisher;
    private readonly Clock _clock;
    private readonly ILogger<OutboxDispatcher> _logger;

    /// <summary>Creates the dispatcher.</summary>
    /// <param name="outboxRepository">Outbox data access.</param>
    /// <param name="publisher">Delivers events (Service Bus or log).</param>
    /// <param name="clock">Current UTC time.</param>
    /// <param name="logger">Logger.</param>
    public OutboxDispatcher(OutboxRepository outboxRepository, IEventPublisher publisher, Clock clock, ILogger<OutboxDispatcher> logger)
    {
        _outboxRepository = outboxRepository;
        _publisher = publisher;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Publishes up to <see cref="BatchSize"/> pending events.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>How many events were published.</returns>
    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _outboxRepository.GetPendingAsync(BatchSize);
        var published = 0;
        foreach (var outboxEvent in pending)
        {
            try
            {
                await _publisher.PublishAsync(outboxEvent, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Publishing {EventType} {EventId} failed; it will be retried.",
                    outboxEvent.EventType, outboxEvent.EventId);
                break;
            }
            outboxEvent.PublishedAtUtc = _clock.UtcNow;
            published++;
        }

        if (published > 0) await _outboxRepository.SaveChangesAsync();
        return published;
    }
}
