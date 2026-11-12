/**
 * @file RequisitionDtos.cs
 * @brief Request and response shapes for the /requisitions endpoints.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>
/// Body of POST /requisitions. Send with an Idempotency-Key header.
/// Hire: departmentCode, roleTitle and positions are required.
/// Termination: staffNumber and reasonCode are required; departmentCode defaults to the employee's
/// home department and roleTitle to their current role.
/// </summary>
/// <param name="Type">Hire or Termination. Defaults to Hire.</param>
/// <param name="DepartmentCode">Requesting department code, e.g. EMS.</param>
/// <param name="RoleTitle">Hire: job title to hire for. Termination: optional.</param>
/// <param name="Positions">Hire: number of people to hire, 1 to 100. Termination: omit (always 1).</param>
/// <param name="ReasonCode">Reason, max 50 characters, e.g. GROWTH, or RESIGNATION / RETIREMENT / DISMISSAL / END_OF_CONTRACT. Required for Termination.</param>
/// <param name="StaffNumber">Termination only: the employee to terminate.</param>
[RequiredFields("departmentCode", "roleTitle", "positions")]
public record CreateRequisitionRequest(
    RequisitionType? Type,
    string? DepartmentCode,
    string? RoleTitle,
    int? Positions,
    string? ReasonCode,
    string? StaffNumber = null);

/// <summary>
/// Body of PATCH /requisitions/{number}. Records a status change and/or budget decision by hand.
/// Hire: Received → HRReview → AwaitingBudget → Approved → Filling → Completed → Closed; Received,
/// HRReview and AwaitingBudget may also move to Rejected; any non-final status may move to Closed.
/// Approved requires BudgetDecision Approved.
/// Termination: Received → HRReview → Approved → Completed → Closed; Received and HRReview may move
/// to Rejected. Moving to Completed carries out the termination. No budgetDecision or positionsFilled.
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
