namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

// All enums are stored as strings (see CmesDbContext.ConfigureConventions) so the
// database stays readable and adding a value never renumbers existing rows.

public enum EmploymentStatus
{
    Active,
    Probation,
    OnLeave,
    Suspended,
    Terminated
}

public enum EmploymentType
{
    FullTime,
    PartTime,
    Contract,
    Seasonal
}

public enum ShiftType
{
    Day,
    Evening,
    Night,
    OnCall
}

public enum RosterStatus
{
    Scheduled,
    Confirmed,
    Completed,
    Cancelled
}

public enum RequisitionType
{
    Hire,
    Termination
}

public enum RequisitionStatus
{
    Received,
    HRReview,
    AwaitingBudget,
    Approved,
    Rejected,
    Filling,
    Completed,
    Closed
}

public enum BudgetDecision
{
    Pending,
    Approved,
    Rejected
}

public enum CredentialStatus
{
    Valid,
    Expiring,
    Expired,
    Suspended
}
