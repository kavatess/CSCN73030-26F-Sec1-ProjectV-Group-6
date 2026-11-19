/**
 * @file RequisitionWorkflow.cs
 * @brief Allowed requisition status transitions, per requisition type.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// Which status may follow which. Rejected and Closed are final for both types.
/// <code>
/// Hire:        Received → HRReview → AwaitingBudget → Approved → Filling → Completed → Closed
///              Received / HRReview / AwaitingBudget → Rejected;  any non-final status → Closed
/// Termination: Received → HRReview → Approved → Completed → Closed
///              Received / HRReview → Rejected;  any non-final status → Closed
/// </code>
/// Completing a termination carries it out (see TerminationService).
/// </summary>
public static class RequisitionWorkflow
{
    private static readonly Dictionary<RequisitionStatus, RequisitionStatus[]> HireMoves = new()
    {
        [RequisitionStatus.Received] = [RequisitionStatus.HRReview, RequisitionStatus.Rejected, RequisitionStatus.Closed],
        [RequisitionStatus.HRReview] = [RequisitionStatus.AwaitingBudget, RequisitionStatus.Rejected, RequisitionStatus.Closed],
        [RequisitionStatus.AwaitingBudget] = [RequisitionStatus.Approved, RequisitionStatus.Rejected, RequisitionStatus.Closed],
        [RequisitionStatus.Approved] = [RequisitionStatus.Filling, RequisitionStatus.Closed],
        [RequisitionStatus.Filling] = [RequisitionStatus.Completed, RequisitionStatus.Closed],
        [RequisitionStatus.Completed] = [RequisitionStatus.Closed],
        [RequisitionStatus.Rejected] = [],
        [RequisitionStatus.Closed] = [],
    };

    private static readonly Dictionary<RequisitionStatus, RequisitionStatus[]> TerminationMoves = new()
    {
        [RequisitionStatus.Received] = [RequisitionStatus.HRReview, RequisitionStatus.Rejected, RequisitionStatus.Closed],
        [RequisitionStatus.HRReview] = [RequisitionStatus.Approved, RequisitionStatus.Rejected, RequisitionStatus.Closed],
        [RequisitionStatus.AwaitingBudget] = [],
        [RequisitionStatus.Approved] = [RequisitionStatus.Completed, RequisitionStatus.Closed],
        [RequisitionStatus.Filling] = [],
        [RequisitionStatus.Completed] = [RequisitionStatus.Closed],
        [RequisitionStatus.Rejected] = [],
        [RequisitionStatus.Closed] = [],
    };

    /// <summary>True when a requisition of <paramref name="type"/> in status <paramref name="from"/> may move to <paramref name="to"/>.</summary>
    /// <param name="type">Hire or Termination.</param>
    /// <param name="from">Current status.</param>
    /// <param name="to">Requested status.</param>
    public static bool CanMove(RequisitionType type, RequisitionStatus from, RequisitionStatus to) =>
        Moves(type)[from].Contains(to);

    /// <summary>The statuses a requisition may move to next; empty for final statuses.</summary>
    /// <param name="type">Hire or Termination.</param>
    /// <param name="from">Current status.</param>
    public static IReadOnlyList<RequisitionStatus> NextStatuses(RequisitionType type, RequisitionStatus from) =>
        Moves(type)[from];

    /// <summary>True for statuses after which a requisition can never change again.</summary>
    /// <param name="status">Status to check.</param>
    public static bool IsFinal(RequisitionStatus status) =>
        status is RequisitionStatus.Rejected or RequisitionStatus.Closed;

    private static Dictionary<RequisitionStatus, RequisitionStatus[]> Moves(RequisitionType type) =>
        type == RequisitionType.Termination ? TerminationMoves : HireMoves;
}
