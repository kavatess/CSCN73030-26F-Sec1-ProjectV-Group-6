/**
 * @file Enums.cs
 * @brief Enumerations shared by the Staff/HR domain, the API contracts and the database.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

// All enums are stored as strings (see CmesDbContext.ConfigureConventions) so the
// database stays readable and adding a value never renumbers existing rows.

/// <summary>Where an employee stands with the city.</summary>
public enum EmploymentStatus
{
    /// <summary>Working normally.</summary>
    Active,
    /// <summary>Newly hired and still in the probation period. New hires default to this.</summary>
    Probation,
    /// <summary>Employed but temporarily away (parental, medical, etc.).</summary>
    OnLeave,
    /// <summary>Temporarily barred from work, e.g. pending an investigation.</summary>
    Suspended,
    /// <summary>No longer employed. The staff number is kept and never reused.</summary>
    Terminated
}

/// <summary>The contract an employee is on.</summary>
public enum EmploymentType
{
    /// <summary>Permanent full-time.</summary>
    FullTime,
    /// <summary>Permanent part-time.</summary>
    PartTime,
    /// <summary>Fixed-term contract.</summary>
    Contract,
    /// <summary>Works only part of the year (e.g. road crews in summer).</summary>
    Seasonal
}

/// <summary>Kind of shift on the duty roster.</summary>
public enum ShiftType
{
    /// <summary>Daytime shift.</summary>
    Day,
    /// <summary>Evening shift.</summary>
    Evening,
    /// <summary>Overnight shift.</summary>
    Night,
    /// <summary>Available to be called in.</summary>
    OnCall
}

/// <summary>Lifecycle of a roster entry. Cancelled shifts are ignored by overlap checks and on-duty counts.</summary>
public enum RosterStatus
{
    /// <summary>Planned; the default for new shifts.</summary>
    Scheduled,
    /// <summary>Confirmed by the staff member or supervisor.</summary>
    Confirmed,
    /// <summary>The shift has been worked.</summary>
    Completed,
    /// <summary>Called off. Does not count as on duty and does not block other shifts.</summary>
    Cancelled
}

/// <summary>What a requisition asks for.</summary>
public enum RequisitionType
{
    /// <summary>Hire one or more people. The only type accepted in this release.</summary>
    Hire,
    /// <summary>Terminate an employee. Reserved for a later release.</summary>
    Termination
}

/// <summary>
/// Where a requisition is in its workflow. Allowed moves are defined in
/// <see cref="RequisitionWorkflow"/>; Rejected and Closed are final.
/// </summary>
public enum RequisitionStatus
{
    /// <summary>Just raised by a department.</summary>
    Received,
    /// <summary>HR is reviewing the request.</summary>
    HRReview,
    /// <summary>Waiting for a decision from Budget &amp; Treasury.</summary>
    AwaitingBudget,
    /// <summary>Funded; recruitment can start.</summary>
    Approved,
    /// <summary>Turned down. Final.</summary>
    Rejected,
    /// <summary>Some positions are being filled.</summary>
    Filling,
    /// <summary>Every position is filled.</summary>
    Completed,
    /// <summary>Closed or withdrawn. Final.</summary>
    Closed
}

/// <summary>Budget &amp; Treasury's answer on a requisition's funding.</summary>
public enum BudgetDecision
{
    /// <summary>Asked, not yet answered.</summary>
    Pending,
    /// <summary>Funded. Required before a requisition can be Approved.</summary>
    Approved,
    /// <summary>Not funded.</summary>
    Rejected
}

/// <summary>
/// A credential's status. Valid, Expiring and Expired are derived from the expiry date on
/// every read; only Suspended is set by hand (see <see cref="CredentialStatusRules"/>).
/// </summary>
public enum CredentialStatus
{
    /// <summary>Expires more than 30 days from today.</summary>
    Valid,
    /// <summary>Expires today or within the next 30 days.</summary>
    Expiring,
    /// <summary>Expiry date is in the past.</summary>
    Expired,
    /// <summary>Suspended by hand; overrides the date-based status.</summary>
    Suspended
}
