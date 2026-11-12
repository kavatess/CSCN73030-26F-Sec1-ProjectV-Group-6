/**
 * @file MessagingOptions.cs
 * @brief Configuration for publishing integration events (section "Messaging").
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Messaging;

/// <summary>
/// Settings from the "Messaging" configuration section. With neither a connection string nor a
/// namespace set, events are written to the log instead of Azure Service Bus.
/// </summary>
public class MessagingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string Section = "Messaging";

    /// <summary>When false, the background publisher does not run; events stay in the outbox.</summary>
    public bool BackgroundPublishing { get; set; } = true;

    /// <summary>Seconds between outbox passes.</summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>Azure Service Bus settings.</summary>
    public ServiceBusSettings ServiceBus { get; set; } = new();

    /// <summary>Azure Service Bus connection settings.</summary>
    public class ServiceBusSettings
    {
        /// <summary>Connection string (local development). Prefer FullyQualifiedNamespace in Azure; keep secrets in Key Vault.</summary>
        public string? ConnectionString { get; set; }

        /// <summary>e.g. cmes.servicebus.windows.net. Authenticates with the app's managed identity.</summary>
        public string? FullyQualifiedNamespace { get; set; }

        /// <summary>Topic that peer services subscribe to.</summary>
        public string TopicName { get; set; } = "staff-events";

        /// <summary>True when either connection setting is present.</summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString) || !string.IsNullOrWhiteSpace(FullyQualifiedNamespace);
    }
}
