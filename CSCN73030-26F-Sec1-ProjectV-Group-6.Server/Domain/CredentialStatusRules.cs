/**
 * @file CredentialStatusRules.cs
 * @brief Derives a credential's status from its expiry date.
 */

using System.Linq.Expressions;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// A credential's status is derived from its expiry date on read: Expired if the expiry date is
/// in the past, Expiring if it is within 30 days, otherwise Valid. A stored Suspended status
/// overrides the derivation.
/// </summary>
/// <remarks>
/// <see cref="Derive(CredentialStatus, DateOnly, DateOnly)"/> is used in C# code. <see cref="IsInState"/> expresses the same rule
/// as a LINQ expression so EF Core can filter by state in SQL. Keep the two in step.
/// </remarks>
/// @par Business rule 5
/// Status is derived on read; Suspended overrides.
public static class CredentialStatusRules
{
    /// <summary>A credential expiring within this many days (inclusive) is Expiring.</summary>
    public const int ExpiringWindowDays = 30;

    /// <summary>Works out the status a credential should show today.</summary>
    /// <param name="storedStatus">The status stored in the database; only Suspended is honoured.</param>
    /// <param name="expiryDate">The credential's last valid day.</param>
    /// <param name="today">Today's date (UTC).</param>
    /// <returns>Suspended, Expired, Expiring or Valid.</returns>
    public static CredentialStatus Derive(CredentialStatus storedStatus, DateOnly expiryDate, DateOnly today)
    {
        if (storedStatus == CredentialStatus.Suspended) return CredentialStatus.Suspended;
        if (expiryDate < today) return CredentialStatus.Expired;
        if (expiryDate <= today.AddDays(ExpiringWindowDays)) return CredentialStatus.Expiring;
        return CredentialStatus.Valid;
    }

    /// <summary>Same as <see cref="Derive(CredentialStatus, DateOnly, DateOnly)"/>, for a loaded credential.</summary>
    /// <param name="credential">The credential.</param>
    /// <param name="today">Today's date (UTC).</param>
    public static CredentialStatus Derive(Credential credential, DateOnly today) =>
        Derive(credential.Status, credential.ExpiryDate, today);

    /// <summary>SQL-translatable filter: credentials whose derived status equals <paramref name="state"/>.</summary>
    /// <param name="state">The derived status to match.</param>
    /// <param name="today">Today's date (UTC).</param>
    public static Expression<Func<Credential, bool>> IsInState(CredentialStatus state, DateOnly today)
    {
        var expiringLimit = today.AddDays(ExpiringWindowDays);
        return state switch
        {
            CredentialStatus.Suspended => c => c.Status == CredentialStatus.Suspended,
            CredentialStatus.Expired => c => c.Status != CredentialStatus.Suspended && c.ExpiryDate < today,
            CredentialStatus.Expiring => c => c.Status != CredentialStatus.Suspended
                                              && c.ExpiryDate >= today && c.ExpiryDate <= expiringLimit,
            _ => c => c.Status != CredentialStatus.Suspended && c.ExpiryDate > expiringLimit,
        };
    }

    /// <summary>
    /// The most urgent status among a staff member's credentials, for directory badges:
    /// Expired, then Suspended, then Expiring, then Valid. Null when they hold none.
    /// </summary>
    /// <param name="statuses">Derived statuses of every credential the person holds.</param>
    public static CredentialStatus? Worst(IEnumerable<CredentialStatus> statuses)
    {
        CredentialStatus[] severityOrder =
            [CredentialStatus.Expired, CredentialStatus.Suspended, CredentialStatus.Expiring, CredentialStatus.Valid];
        var set = statuses.ToHashSet();
        foreach (var status in severityOrder)
        {
            if (set.Contains(status)) return status;
        }
        return null;
    }
}
