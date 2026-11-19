/**
 * @file DutyEligibilityRules.cs
 * @brief Decides whether a staff member may work a shift.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>One reason or warning in an eligibility decision.</summary>
/// <param name="Code">Stable machine-readable code, e.g. CREDENTIAL_EXPIRED.</param>
/// <param name="Message">Human-readable explanation.</param>
public record EligibilityIssue(string Code, string Message);

/// <summary>The outcome of <see cref="DutyEligibilityRules.Evaluate"/>.</summary>
/// <param name="Reasons">Why the person is not eligible; empty when eligible.</param>
/// <param name="Warnings">Things that do not block duty yet, e.g. a credential expiring soon.</param>
public record EligibilityDecision(IReadOnlyList<EligibilityIssue> Reasons, IReadOnlyList<EligibilityIssue> Warnings)
{
    /// <summary>True when nothing blocks duty.</summary>
    public bool Eligible => Reasons.Count == 0;
}

/// <summary>
/// Duty eligibility. A person is eligible for duty on a day when ALL of these hold:
/// <list type="number">
/// <item>employment status is Active or Probation;</item>
/// <item>they have an assignment current on that day (in the requested service, if one is given);</item>
/// <item>no duty-blocking credential they hold is Expired or Suspended on that day;</item>
/// <item>no security flag on them is Active.</item>
/// </list>
/// A duty-blocking credential that is Expiring is a warning, not a reason.
/// </summary>
public static class DutyEligibilityRules
{
    /// <summary>Codes used in <see cref="EligibilityIssue.Code"/>.</summary>
    public static class Codes
    {
        /// <summary>Status is OnLeave, Suspended or Terminated.</summary>
        public const string EmploymentStatus = "EMPLOYMENT_STATUS";
        /// <summary>No assignment covers the day (in the requested service).</summary>
        public const string NoCurrentAssignment = "NO_CURRENT_ASSIGNMENT";
        /// <summary>A duty-blocking credential has expired.</summary>
        public const string CredentialExpired = "CREDENTIAL_EXPIRED";
        /// <summary>A duty-blocking credential is suspended.</summary>
        public const string CredentialSuspended = "CREDENTIAL_SUSPENDED";
        /// <summary>The Security service has an Active flag on the person.</summary>
        public const string SecurityFlagActive = "SECURITY_FLAG_ACTIVE";
        /// <summary>Warning: a duty-blocking credential expires within 30 days.</summary>
        public const string CredentialExpiring = "CREDENTIAL_EXPIRING";
    }

    /// <summary>Evaluates eligibility. Needs the staff member's Assignments (with Service) and Credentials (with CredentialType) loaded.</summary>
    /// <param name="staff">The staff member.</param>
    /// <param name="flags">Their security flags.</param>
    /// <param name="day">The day being checked (UTC).</param>
    /// <param name="serviceId">Only count assignments in this service, or null for any service.</param>
    public static EligibilityDecision Evaluate(Staff staff, IEnumerable<SecurityFlag> flags, DateOnly day, int? serviceId)
    {
        var reasons = new List<EligibilityIssue>();
        var warnings = new List<EligibilityIssue>();

        if (staff.EmploymentStatus is not (EmploymentStatus.Active or EmploymentStatus.Probation))
            reasons.Add(new(Codes.EmploymentStatus, $"Employment status is {staff.EmploymentStatus}."));

        var current = staff.Assignments.Where(a => a.IsCurrentOn(day) && (serviceId == null || a.ServiceId == serviceId));
        if (!current.Any())
            reasons.Add(new(Codes.NoCurrentAssignment, serviceId == null
                ? $"No assignment covers {day:yyyy-MM-dd}."
                : $"No assignment in service {serviceId} covers {day:yyyy-MM-dd}."));

        foreach (var credential in staff.Credentials.Where(c => c.CredentialType.IsDutyBlocking).OrderBy(c => c.ExpiryDate))
        {
            var name = $"{credential.CredentialType.Name} {credential.Number}";
            switch (CredentialStatusRules.Derive(credential, day))
            {
                case CredentialStatus.Expired:
                    reasons.Add(new(Codes.CredentialExpired, $"{name} expired on {credential.ExpiryDate:yyyy-MM-dd}."));
                    break;
                case CredentialStatus.Suspended:
                    reasons.Add(new(Codes.CredentialSuspended, $"{name} is suspended."));
                    break;
                case CredentialStatus.Expiring:
                    warnings.Add(new(Codes.CredentialExpiring, $"{name} expires on {credential.ExpiryDate:yyyy-MM-dd}."));
                    break;
            }
        }

        foreach (var flag in flags.Where(f => f.Status == SecurityFlagStatus.Active).OrderBy(f => f.FlagType))
            reasons.Add(new(Codes.SecurityFlagActive, $"Security flag {flag.FlagType} is active."));

        return new EligibilityDecision(reasons, warnings);
    }
}
