/**
 * @file HealthApiTests.cs
 * @brief Regression tests for GET /health.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>GET /health reports the service, its database and the integration-event backlog.</summary>
    public class HealthApiTests : ApiTestBase
    {
        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public HealthApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Health_WithDatabaseUp_Returns200Healthy()
        {
            await DispatchOutboxAsync();
            var health = await ReadAsync<HealthDto>(await Client.GetAsync("/health"), HttpStatusCode.OK);

            Assert.Equal("Healthy", health.Status);
            Assert.Equal("Healthy", health.Checks["database"]);
            Assert.Equal("Healthy", health.Checks["outbox"]);
        }

        [Fact]
        public async Task Health_WithEventsStuckInTheOutbox_IsDegradedButStill200()
        {
            await DispatchOutboxAsync();
            using (var scope = Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CmesDbContext>();
                db.OutboxEvents.Add(new OutboxEvent
                {
                    EventId = Guid.NewGuid(), EventType = "Test.Stuck", SchemaVersion = 1, PayloadJson = "{}",
                    OccurredAtUtc = StaffHrApiFactory.Now.UtcDateTime.AddMinutes(-10),
                });
                await db.SaveChangesAsync();
            }

            try
            {
                var health = await ReadAsync<HealthDto>(await Client.GetAsync("/health"), HttpStatusCode.OK);
                Assert.Equal("Degraded", health.Status);
                Assert.Equal("Degraded", health.Checks["outbox"]);
                Assert.Equal("Healthy", health.Checks["database"]);
            }
            finally
            {
                await DispatchOutboxAsync(); // publish it so other tests see a clean outbox
            }
        }
    }
}
