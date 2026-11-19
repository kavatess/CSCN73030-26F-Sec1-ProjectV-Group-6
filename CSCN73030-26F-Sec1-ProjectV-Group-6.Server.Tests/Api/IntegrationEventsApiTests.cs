/**
 * @file IntegrationEventsApiTests.cs
 * @brief Regression tests for the transactional outbox and Staff.Onboarded publishing.
 */

using System.Net;
using System.Text.Json;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Onboarding writes a Staff.Onboarded event in the same transaction; the dispatcher publishes it once, in order.</summary>
    public class IntegrationEventsApiTests : ApiTestBase
    {
        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public IntegrationEventsApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Onboarding_WritesOneUnpublishedStaffOnboardedEvent()
        {
            var serviceId = await ServiceIdAsync("HRS", "Recruitment");
            var staff = await CreateStaffAsync(new CreateAssignmentRequest(serviceId, "Recruiter", null, null));

            var outboxEvent = Assert.Single(await OutboxEventsForAsync(staff.StaffNumber));

            Assert.Equal(IntegrationEventTypes.StaffOnboarded, outboxEvent.EventType);
            Assert.Equal(IntegrationEventTypes.SchemaVersion, outboxEvent.SchemaVersion);
            Assert.Null(outboxEvent.PublishedAtUtc);
            Assert.Equal(StaffHrApiFactory.Now.UtcDateTime, outboxEvent.OccurredAtUtc);

            var payload = JsonDocument.Parse(outboxEvent.PayloadJson).RootElement;
            Assert.Equal(staff.StaffNumber, payload.GetProperty("staffNumber").GetString());
            Assert.Equal("HRS", payload.GetProperty("departmentCode").GetString());
            Assert.Equal("Probation", payload.GetProperty("employmentStatus").GetString());
            Assert.Equal(serviceId, payload.GetProperty("serviceId").GetInt32());
            Assert.Equal("Recruiter", payload.GetProperty("roleTitle").GetString());
        }

        [Fact]
        public async Task OnboardedPayload_LeavesOutPersonalAndPayData()
        {
            var staff = await CreateStaffAsync();
            var payload = Assert.Single(await OutboxEventsForAsync(staff.StaffNumber)).PayloadJson;

            Assert.DoesNotContain("dateOfBirth", payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("citizenId", payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("salary", payload, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FailedOnboarding_WritesNoEvent()
        {
            var email = UniqueEmail();
            await CreateStaffAsync(); // something else in the outbox
            await PostAsync("/staff", NewStaffRequest() with { Email = email });
            var duplicate = await PostAsync("/staff", NewStaffRequest() with { Email = email });
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            // Only the first, successful onboarding wrote an event.
            Assert.Single(await OutboxEventsForAsync(email));
        }

        [Fact]
        public async Task Dispatcher_PublishesPendingEventsAndMarksThem()
        {
            var staff = await CreateStaffAsync();

            var published = await DispatchOutboxAsync();

            Assert.True(published >= 1);
            var outboxEvent = Assert.Single(await OutboxEventsForAsync(staff.StaffNumber));
            Assert.Equal(StaffHrApiFactory.Now.UtcDateTime, outboxEvent.PublishedAtUtc);
            Assert.Single(Factory.Events.Published, e => e.EventId == outboxEvent.EventId);
        }

        [Fact]
        public async Task Dispatcher_DoesNotPublishTheSameEventTwice()
        {
            var staff = await CreateStaffAsync();
            await DispatchOutboxAsync();

            Assert.Equal(0, await DispatchOutboxAsync());

            var eventId = Assert.Single(await OutboxEventsForAsync(staff.StaffNumber)).EventId;
            Assert.Single(Factory.Events.Published, e => e.EventId == eventId);
        }

        [Fact]
        public async Task Dispatcher_WhenBrokerIsDown_KeepsEventsAndRetriesLater()
        {
            await DispatchOutboxAsync();
            var staff = await CreateStaffAsync();

            Factory.Events.Unavailable = true;
            try
            {
                Assert.Equal(0, await DispatchOutboxAsync());
                Assert.Null(Assert.Single(await OutboxEventsForAsync(staff.StaffNumber)).PublishedAtUtc);
            }
            finally
            {
                Factory.Events.Unavailable = false;
            }

            Assert.Equal(1, await DispatchOutboxAsync());
            Assert.NotNull(Assert.Single(await OutboxEventsForAsync(staff.StaffNumber)).PublishedAtUtc);
        }

        [Fact]
        public async Task Dispatcher_PublishesInTheOrderEventsHappened()
        {
            await DispatchOutboxAsync();
            var now = StaffHrApiFactory.Now.UtcDateTime;
            OutboxEvent Make(string label, int secondsLater) => new()
            {
                EventId = Guid.NewGuid(), EventType = "Test.Ordering", SchemaVersion = 1,
                PayloadJson = $"{{\"label\":\"{label}\"}}", OccurredAtUtc = now.AddSeconds(secondsLater),
            };
            using (var scope = Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CmesDbContext>();
                db.OutboxEvents.AddRange(Make("later", 2), Make("earlier", 1)); // inserted out of order
                await db.SaveChangesAsync();
            }

            var before = Factory.Events.Published.Count;
            await DispatchOutboxAsync();

            var labels = Factory.Events.Published.Skip(before)
                .Select(e => JsonDocument.Parse(e.PayloadJson).RootElement.GetProperty("label").GetString());
            Assert.Equal(["earlier", "later"], labels);
        }
    }
}
