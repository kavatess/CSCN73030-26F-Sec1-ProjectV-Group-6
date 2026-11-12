/**
 * @file ServiceBusEventPublisher.cs
 * @brief Publishes outbox events to an Azure Service Bus topic.
 */

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.Extensions.Options;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Messaging;

/// <summary>
/// Sends each event as one message to the configured topic. MessageId is the event id, so a
/// topic with duplicate detection drops a re-sent event; Subject is the event type
/// (e.g. Staff.Onboarded) so subscriptions can filter on it.
/// </summary>
public sealed class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    /// <summary>Connects with the connection string, or with the managed identity when only a namespace is given.</summary>
    /// <param name="options">Messaging settings.</param>
    public ServiceBusEventPublisher(IOptions<MessagingOptions> options)
    {
        var settings = options.Value.ServiceBus;
        _client = !string.IsNullOrWhiteSpace(settings.ConnectionString)
            ? new ServiceBusClient(settings.ConnectionString)
            : new ServiceBusClient(settings.FullyQualifiedNamespace, new DefaultAzureCredential());
        _sender = _client.CreateSender(settings.TopicName);
    }

    /// <summary>Sends one event.</summary>
    /// <param name="outboxEvent">The event row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task PublishAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken) =>
        _sender.SendMessageAsync(ToMessage(outboxEvent), cancellationToken);

    /// <summary>Builds the Service Bus message for an event. Public so the mapping can be tested.</summary>
    /// <param name="outboxEvent">The event row.</param>
    public static ServiceBusMessage ToMessage(OutboxEvent outboxEvent)
    {
        var message = new ServiceBusMessage(BinaryData.FromString(outboxEvent.PayloadJson))
        {
            MessageId = outboxEvent.EventId.ToString(),
            Subject = outboxEvent.EventType,
            ContentType = "application/json",
        };
        message.ApplicationProperties["eventType"] = outboxEvent.EventType;
        message.ApplicationProperties["schemaVersion"] = outboxEvent.SchemaVersion;
        message.ApplicationProperties["occurredAtUtc"] = outboxEvent.OccurredAtUtc.ToString("O");
        message.ApplicationProperties["source"] = "staff-hr";
        return message;
    }

    /// <summary>Closes the sender and client.</summary>
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
