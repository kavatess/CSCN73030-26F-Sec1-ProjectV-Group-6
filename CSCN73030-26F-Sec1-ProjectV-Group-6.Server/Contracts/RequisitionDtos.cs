/**
 * @file RequisitionDtos.cs
 * @brief Request and response shapes for the /requisitions endpoints.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of POST /requisitions. Send with an Idempotency-Key header.</summary>
/// <param name="Type">Only Hire is supported in this release. Defaults to Hire.</param>
/// <param name="DepartmentCode">Required. Requesting department code, e.g. EMS.</param>
/// <param name="RoleTitle">Required. Job title to hire for.</param>
/// <param name="Positions">Required. Number of people to hire, 1 to 100.</param>
/// <param name="ReasonCode">Optional reason, e.g. GROWTH, max 50 characters.</param>
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
/// <param name="Status">New status; must be an allowed move.</param>
/// <param name="BudgetDecision">Budget &amp; Treasury's decision.</param>
/// <param name="PositionsFilled">0 to Positions. Completed requires every position filled.</param>
public record UpdateRequisitionRequest(
    RequisitionStatus? Status,
    BudgetDecision? BudgetDecision,
    int? PositionsFilled);

/// <summary>Query string of GET /requisitions. Every filter is optional.</summary>
public class RequisitionQuery
{
    /// <summary>Hire or Termination.</summary>
    public RequisitionType? Type { get; set; }

    /// <summary>Workflow status to match.</summary>
    public RequisitionStatus? Status { get; set; }

    /// <summary>Requesting department code, e.g. EMS.</summary>
    public string? Department { get; set; }
}

/// <summary>A requisition as returned by the API.</summary>
/// <param name="RequisitionNumber">REQ-0000000.</param>
/// <param name="Type">Hire or Termination.</param>
/// <param name="Department">Requesting department.</param>
/// <param name="RoleTitle">Job title.</param>
/// <param name="Positions">Number of people to hire.</param>
/// <param name="PositionsFilled">How many are filled.</param>
/// <param name="Status">Workflow status.</param>
/// <param name="ReasonCode">Reason, or null.</param>
/// <param name="BudgetDecision">Budget decision, or null before Budget is asked.</param>
/// <param name="StaffNumber">Employee concerned (termination only), or null.</param>
/// <param name="CreatedAtUtc">When it was raised, UTC.</param>
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
