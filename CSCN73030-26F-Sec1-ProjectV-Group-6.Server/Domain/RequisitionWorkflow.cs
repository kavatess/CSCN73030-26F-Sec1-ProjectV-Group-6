/**
 * @file RequisitionWorkflow.cs
 * @brief Allowed requisition status transitions.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// Which requisition status may follow which. Rejected and Closed are final.
/// <code>
/// Received → HRReview → AwaitingBudget → Approved → Filling → Completed → Closed
///     ↘ Rejected (from Received, HRReview, AwaitingBudget)
///     ↘ Closed   (from any non-final status, e.g. withdrawn by the department)
/// </code>
/// </summary>
public static class RequisitionWorkflow
{
    private static readonly Dictionary<RequisitionStatus, RequisitionStatus[]> Allowed = new()
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

    /// <summary>True when a requisition in status <paramref name="from"/> may move to <paramref name="to"/>.</summary>
    /// <param name="from">Current status.</param>
    /// <param name="to">Requested status.</param>
    public static bool CanMove(RequisitionStatus from, RequisitionStatus to) => Allowed[from].Contains(to);

    /// <summary>The statuses a requisition in <paramref name="from"/> may move to; empty for final statuses.</summary>
    /// <param name="from">Current status.</param>
    public static IReadOnlyList<RequisitionStatus> NextStatuses(RequisitionStatus from) => Allowed[from];
}
