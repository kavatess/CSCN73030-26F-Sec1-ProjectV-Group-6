/**
 * @file RecordingEventPublisher.cs
 * @brief Test double for IEventPublisher that records events and can simulate an outage.
 */

using System.Collections.Concurrent;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Stands in for Azure Service Bus in the API tests.</summary>
    public class RecordingEventPublisher : IEventPublisher
    {
        /// <summary>Every event published so far, in order.</summary>
        public ConcurrentQueue<OutboxEvent> Published { get; } = new();

        /// <summary>While true, every publish throws, as if Service Bus were down.</summary>
        public bool Unavailable { get; set; }

        /// <summary>Records the event, or throws while <see cref="Unavailable"/>.</summary>
        /// <param name="outboxEvent">The event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task PublishAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
        {
            if (Unavailable) throw new InvalidOperationException("Service Bus is unavailable (simulated).");
            Published.Enqueue(outboxEvent);
            return Task.CompletedTask;
        }
    }
}
