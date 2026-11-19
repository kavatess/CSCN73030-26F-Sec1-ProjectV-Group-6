/**
 * @file ServiceBusMessageTests.cs
 * @brief Unit tests for how outbox events map to Azure Service Bus messages.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Messaging;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>Message id, subject, body and properties of a published event.</summary>
    public class ServiceBusMessageTests
    {
        private static readonly OutboxEvent Event = new()
        {
            EventId = Guid.Parse("6f1c2c4e-8f3a-4d55-9a0b-0b7d4c1e2f11"),
            EventType = "Staff.Onboarded",
            SchemaVersion = 1,
            PayloadJson = "{\"staffNumber\":\"STF-0000021\"}",
            OccurredAtUtc = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc),
        };

        [Fact]
        public void ToMessage_UsesTheEventIdForDuplicateDetection()
        {
            Assert.Equal("6f1c2c4e-8f3a-4d55-9a0b-0b7d4c1e2f11", ServiceBusEventPublisher.ToMessage(Event).MessageId);
        }

        [Fact]
        public void ToMessage_CarriesTypeVersionAndPayload()
        {
            var message = ServiceBusEventPublisher.ToMessage(Event);

            Assert.Equal("Staff.Onboarded", message.Subject);
            Assert.Equal("application/json", message.ContentType);
            Assert.Equal("{\"staffNumber\":\"STF-0000021\"}", message.Body.ToString());
            Assert.Equal("Staff.Onboarded", message.ApplicationProperties["eventType"]);
            Assert.Equal(1, message.ApplicationProperties["schemaVersion"]);
            Assert.Equal("staff-hr", message.ApplicationProperties["source"]);
        }

        [Fact]
        public void MessagingOptions_AreConfiguredByConnectionStringOrNamespace()
        {
            Assert.False(new MessagingOptions.ServiceBusSettings().IsConfigured);
            Assert.True(new MessagingOptions.ServiceBusSettings { ConnectionString = "Endpoint=sb://x/" }.IsConfigured);
            Assert.True(new MessagingOptions.ServiceBusSettings { FullyQualifiedNamespace = "cmes.servicebus.windows.net" }.IsConfigured);
            Assert.Equal("staff-events", new MessagingOptions.ServiceBusSettings().TopicName);
        }
    }
}
