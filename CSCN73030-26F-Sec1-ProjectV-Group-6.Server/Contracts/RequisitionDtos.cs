using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of POST /requisitions. Send with an Idempotency-Key header.</summary>
/// <param name="Type">Only Hire is supported in this release. Defaults to Hire.</param>
/// <param name="DepartmentCode">Requesting department code, e.g. EMS.</param>
/// <param name="Positions">Number of people to hire, 1 to 100.</param>
[RequiredFields("departmentCode", "roleTitle", "positions")]
public record CreateRequisitionRequest(
    RequisitionType? Type,
    string? DepartmentCode,
    string? RoleTitle,
    int? Positions,
    string? ReasonCode);

/// <summary>
/// Body of PATCH /requisitions/{number}. Records a status change and/or budget decision by hand.
/// Allowed status moves: Received → HRReview → AwaitingBudget → Approved → Filling → Completed → Closed;
/// Received, HRReview and AwaitingBudget may also move to Rejected; any non-final status may move to Closed.
/// Approved requires BudgetDecision Approved.
/// </summary>
public record UpdateRequisitionRequest(
    RequisitionStatus? Status,
    BudgetDecision? BudgetDecision,
    int? PositionsFilled);

public class RequisitionQuery
{
    public RequisitionType? Type { get; set; }
    public RequisitionStatus? Status { get; set; }

    /// <summary>Requesting department code, e.g. EMS.</summary>
    public string? Department { get; set; }
}

public record RequisitionDto(
    string RequisitionNumber,
    RequisitionType Type,
    DepartmentRefDto Department,
    string RoleTitle,
    int Positions,
    int PositionsFilled,
    RequisitionStatus Status,
    string? ReasonCode,
    BudgetDecision? BudgetDecision,
    string? StaffNumber,
    DateTime CreatedAtUtc);
