using System.Linq.Expressions;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// Business rule 5: a credential's status is derived from its expiry date on read.
/// Expired if the expiry date is in the past, Expiring if it is within 30 days,
/// otherwise Valid. A stored Suspended status overrides the derivation.
/// </summary>
/// <remarks>
/// <see cref="Derive(CredentialStatus, DateOnly, DateOnly)"/> is used in C# code. <see cref="IsInState"/> expresses the same rule
/// as a LINQ expression so EF Core can filter by state in SQL. Keep the two in step.
/// </remarks>
public static class CredentialStatusRules
{
    public const int ExpiringWindowDays = 30;

    public static CredentialStatus Derive(CredentialStatus storedStatus, DateOnly expiryDate, DateOnly today)
    {
        if (storedStatus == CredentialStatus.Suspended) return CredentialStatus.Suspended;
        if (expiryDate < today) return CredentialStatus.Expired;
        if (expiryDate <= today.AddDays(ExpiringWindowDays)) return CredentialStatus.Expiring;
        return CredentialStatus.Valid;
    }

    public static CredentialStatus Derive(Credential credential, DateOnly today) =>
        Derive(credential.Status, credential.ExpiryDate, today);

    /// <summary>SQL-translatable filter: credentials whose derived status equals <paramref name="state"/>.</summary>
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
