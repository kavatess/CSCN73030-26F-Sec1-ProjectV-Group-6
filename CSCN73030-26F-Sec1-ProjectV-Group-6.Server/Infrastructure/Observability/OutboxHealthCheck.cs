/**
 * @file OutboxHealthCheck.cs
 * @brief Reports Degraded when integration events are stuck in the outbox.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Observability;

/// <summary>
/// Degraded when an event has waited more than <see cref="MaxAge"/> to be published, which usually
/// means Service Bus is unreachable. The API keeps working; peer services just hear about changes late.
/// </summary>
public class OutboxHealthCheck : IHealthCheck
{
    /// <summary>How long an event may wait before the check reports Degraded.</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

    private readonly CmesDbContext _db;
    private readonly Clock _clock;

    /// <summary>Creates the check.</summary>
    /// <param name="db">Database context.</param>
    /// <param name="clock">Current UTC time.</param>
    public OutboxHealthCheck(CmesDbContext db, Clock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>Counts unpublished events older than <see cref="MaxAge"/>.</summary>
    /// <param name="context">Health check context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cutoff = _clock.UtcNow - MaxAge;
        var stuck = await _db.OutboxEvents.CountAsync(e => e.PublishedAtUtc == null && e.OccurredAtUtc < cutoff, cancellationToken);
        return stuck == 0
            ? HealthCheckResult.Healthy("No events waiting longer than 5 minutes.")
            : HealthCheckResult.Degraded($"{stuck} integration event(s) waiting more than {MaxAge.TotalMinutes:0} minutes to be published.");
    }
}
