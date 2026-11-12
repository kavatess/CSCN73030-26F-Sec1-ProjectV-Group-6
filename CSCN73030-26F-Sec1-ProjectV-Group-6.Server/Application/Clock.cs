/**
 * @file Clock.cs
 * @brief Current UTC time and date for the application.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;

/// <summary>
/// "Now" and "today" in UTC, from the injected TimeProvider (so tests or demos can fake the date later).
/// </summary>
public class Clock
{
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the Clock. Dependencies are supplied by dependency injection.</summary>
    /// <param name="timeProvider">Source of the current time.</param>
    public Clock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>The current instant, UTC.</summary>
    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>Today's date in UTC.</summary>
    public DateOnly Today => DateOnly.FromDateTime(UtcNow);

    /// <summary>
    /// Treats a client-supplied time as UTC: values with an offset are converted, values without
    /// one (Kind = Unspecified) are taken to already be UTC.
    /// </summary>
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
