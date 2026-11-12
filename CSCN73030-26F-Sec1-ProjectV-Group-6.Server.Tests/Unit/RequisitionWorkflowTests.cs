/**
 * @file RequisitionWorkflowTests.cs
 * @brief Unit tests for the allowed requisition status transitions.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>Received → HRReview → AwaitingBudget → Approved → Filling → Completed → Closed, plus Rejected and Closed exits.</summary>
    public class RequisitionWorkflowTests
    {
        /// <summary>Every allowed move, written out so a change to the workflow is a deliberate test change.</summary>
        public static readonly TheoryData<RequisitionStatus, RequisitionStatus> AllowedMoves = new()
        {
            { RequisitionStatus.Received, RequisitionStatus.HRReview },
            { RequisitionStatus.Received, RequisitionStatus.Rejected },
            { RequisitionStatus.Received, RequisitionStatus.Closed },
            { RequisitionStatus.HRReview, RequisitionStatus.AwaitingBudget },
            { RequisitionStatus.HRReview, RequisitionStatus.Rejected },
            { RequisitionStatus.HRReview, RequisitionStatus.Closed },
            { RequisitionStatus.AwaitingBudget, RequisitionStatus.Approved },
            { RequisitionStatus.AwaitingBudget, RequisitionStatus.Rejected },
            { RequisitionStatus.AwaitingBudget, RequisitionStatus.Closed },
            { RequisitionStatus.Approved, RequisitionStatus.Filling },
            { RequisitionStatus.Approved, RequisitionStatus.Closed },
            { RequisitionStatus.Filling, RequisitionStatus.Completed },
            { RequisitionStatus.Filling, RequisitionStatus.Closed },
            { RequisitionStatus.Completed, RequisitionStatus.Closed },
        };

        [Theory]
        [MemberData(nameof(AllowedMoves))]
        public void CanMove_AllowsTheDocumentedMoves(RequisitionStatus from, RequisitionStatus to)
        {
            Assert.True(RequisitionWorkflow.CanMove(from, to));
        }

        [Fact]
        public void CanMove_RejectsEveryOtherMove()
        {
            var allowed = AllowedMoves.Select(row => ((RequisitionStatus)row[0], (RequisitionStatus)row[1])).ToHashSet();
            foreach (var from in Enum.GetValues<RequisitionStatus>())
            foreach (var to in Enum.GetValues<RequisitionStatus>())
            {
                if (!allowed.Contains((from, to)))
                    Assert.False(RequisitionWorkflow.CanMove(from, to), $"{from} → {to} should not be allowed");
            }
        }

        [Theory]
        [InlineData(RequisitionStatus.Rejected)]
        [InlineData(RequisitionStatus.Closed)]
        public void NextStatuses_IsEmptyForFinalStatuses(RequisitionStatus final)
        {
            Assert.Empty(RequisitionWorkflow.NextStatuses(final));
        }

        [Fact]
        public void NextStatuses_ListsTheAllowedMovesFromReceived()
        {
            Assert.Equal(
                [RequisitionStatus.HRReview, RequisitionStatus.Rejected, RequisitionStatus.Closed],
                RequisitionWorkflow.NextStatuses(RequisitionStatus.Received));
        }

        [Fact]
        public void NextStatuses_IsDefinedForEveryStatus()
        {
            foreach (var status in Enum.GetValues<RequisitionStatus>())
                Assert.NotNull(RequisitionWorkflow.NextStatuses(status));
        }
    }
}
