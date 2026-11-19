namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>Whether a person currently works for the city.</summary>
public enum EmploymentStatus { Active, OnLeave, Terminated }

/// <summary>Whether a request is to hire or to terminate.</summary>
public enum RequisitionType { Hire, Termination }

/// <summary>Where a request is in its life cycle.</summary>
public enum RequisitionStatus { Submitted, PendingBudget, Approved, Rejected, Completed }

/// <summary>Budget's answer to a hire request.</summary>
public enum BudgetDecisionResult { Approved, Rejected }

/// <summary>Whether a certification or licence currently counts.</summary>
public enum CredentialStatus { Valid, Expired, Revoked, Suspended }

/// <summary>Whether a flag raised by Security is still in force.</summary>
public enum SecurityFlagStatus { Active, Cleared }
