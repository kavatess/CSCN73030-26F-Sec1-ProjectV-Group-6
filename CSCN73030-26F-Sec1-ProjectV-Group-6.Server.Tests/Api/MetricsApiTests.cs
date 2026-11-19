/**
 * @file MetricsApiTests.cs
 * @brief Regression tests for the business counters on the CmesStaffHr meter.
 */

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Observability;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Onboarding and event publishing increment their counters, tagged by department / event type.</summary>
    public sealed class MetricsApiTests : ApiTestBase, IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly ConcurrentQueue<(string Instrument, long Value, string? Tag)> _measurements = new();

        /// <summary>Starts listening to the CmesStaffHr meter.</summary>
        /// <param name="factory">The shared app.</param>
        public MetricsApiTests(StaffHrApiFactory factory) : base(factory)
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == StaffHrMetrics.MeterName) listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            {
                string? tag = null;
                foreach (var t in tags) tag = t.Value?.ToString();
                _measurements.Enqueue((instrument.Name, value, tag));
            });
            _listener.Start();
        }

        [Fact]
        public async Task Onboarding_CountsStaffOnboardedForTheDepartment()
        {
            await CreateStaffAsync();

            Assert.Contains(_measurements, m => m is { Instrument: "staffhr.staff.onboarded", Value: 1, Tag: "HRS" });
        }

        [Fact]
        public async Task Publishing_CountsEventsPublished()
        {
            await CreateStaffAsync();
            await DispatchOutboxAsync();

            Assert.Contains(_measurements, m => m is { Instrument: "staffhr.events.published", Tag: "Staff.Onboarded" });
        }

        [Fact]
        public async Task FailedPublish_CountsAFailure()
        {
            await DispatchOutboxAsync();
            await CreateStaffAsync();
            Factory.Events.Unavailable = true;
            try
            {
                await DispatchOutboxAsync();
            }
            finally
            {
                Factory.Events.Unavailable = false;
            }
            await DispatchOutboxAsync();

            Assert.Contains(_measurements, m => m.Instrument == "staffhr.events.publish_failures");
        }

        /// <summary>Stops listening.</summary>
        public void Dispose() => _listener.Dispose();
    }
}
