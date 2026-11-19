/**
 * @file StaffHrMetrics.cs
 * @brief Business counters exported through OpenTelemetry.
 */

using System.Diagnostics.Metrics;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Observability;

/// <summary>
/// Counters on the "CmesStaffHr" meter. With Application Insights configured they appear as custom
/// metrics; locally they can be watched with <c>dotnet-counters monitor --counters CmesStaffHr</c>.
/// </summary>
public sealed class StaffHrMetrics
{
    /// <summary>Meter name to subscribe to.</summary>
    public const string MeterName = "CmesStaffHr";

    private readonly Counter<long> _staffOnboarded;
    private readonly Counter<long> _staffTerminated;
    private readonly Counter<long> _eventsPublished;
    private readonly Counter<long> _eventPublishFailures;

    /// <summary>Creates the counters.</summary>
    /// <param name="meterFactory">Creates the meter (disposed with the app).</param>
    public StaffHrMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _staffOnboarded = meter.CreateCounter<long>("staffhr.staff.onboarded", description: "Employees onboarded.");
        _staffTerminated = meter.CreateCounter<long>("staffhr.staff.terminated", description: "Employees terminated.");
        _eventsPublished = meter.CreateCounter<long>("staffhr.events.published", description: "Integration events published.");
        _eventPublishFailures = meter.CreateCounter<long>("staffhr.events.publish_failures", description: "Failed publish attempts (retried).");
    }

    /// <summary>Counts one onboarding.</summary>
    /// <param name="departmentCode">Home department, recorded as a tag.</param>
    public void StaffOnboarded(string departmentCode) => _staffOnboarded.Add(1, new KeyValuePair<string, object?>("department", departmentCode));

    /// <summary>Counts one termination.</summary>
    /// <param name="departmentCode">Home department, recorded as a tag.</param>
    public void StaffTerminated(string departmentCode) => _staffTerminated.Add(1, new KeyValuePair<string, object?>("department", departmentCode));

    /// <summary>Counts one published event.</summary>
    /// <param name="eventType">Event type, recorded as a tag.</param>
    public void EventPublished(string eventType) => _eventsPublished.Add(1, new KeyValuePair<string, object?>("event_type", eventType));

    /// <summary>Counts one failed publish attempt.</summary>
    /// <param name="eventType">Event type, recorded as a tag.</param>
    public void EventPublishFailed(string eventType) => _eventPublishFailures.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
}
