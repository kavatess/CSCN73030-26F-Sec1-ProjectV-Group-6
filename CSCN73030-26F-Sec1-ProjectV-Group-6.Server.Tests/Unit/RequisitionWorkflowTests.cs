/**
 * @file RequisitionWorkflowTests.cs
 * @brief Unit tests for the allowed requisition status transitions of both requisition types.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>
    /// Hire: Received → HRReview → AwaitingBudget → Approved → Filling → Completed → Closed.
    /// Termination: Received → HRReview → Approved → Completed → Closed. Both have Rejected and Closed exits.
    /// </summary>
    public class RequisitionWorkflowTests
    {
        /// <summary>Every allowed Hire move, written out so a change to the workflow is a deliberate test change.</summary>
        public static readonly TheoryData<RequisitionStatus, RequisitionStatus> HireMoves = new()
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

        /// <summary>Every allowed Termination move: no budget step and no filling.</summary>
        public static readonly TheoryData<RequisitionStatus, RequisitionStatus> TerminationMoves = new()
        {
            { RequisitionStatus.Received, RequisitionStatus.HRReview },
            { RequisitionStatus.Received, RequisitionStatus.Rejected },
            { RequisitionStatus.Received, RequisitionStatus.Closed },
            { RequisitionStatus.HRReview, RequisitionStatus.Approved },
            { RequisitionStatus.HRReview, RequisitionStatus.Rejected },
            { RequisitionStatus.HRReview, RequisitionStatus.Closed },
            { RequisitionStatus.Approved, RequisitionStatus.Completed },
            { RequisitionStatus.Approved, RequisitionStatus.Closed },
            { RequisitionStatus.Completed, RequisitionStatus.Closed },
        };

        [Theory]
        [MemberData(nameof(HireMoves))]
        public void CanMove_Hire_AllowsTheDocumentedMoves(RequisitionStatus from, RequisitionStatus to)
        {
            Assert.True(RequisitionWorkflow.CanMove(RequisitionType.Hire, from, to));
        }

        [Theory]
        [MemberData(nameof(TerminationMoves))]
        public void CanMove_Termination_AllowsTheDocumentedMoves(RequisitionStatus from, RequisitionStatus to)
        {
            Assert.True(RequisitionWorkflow.CanMove(RequisitionType.Termination, from, to));
        }

        [Theory]
        [InlineData(RequisitionType.Hire)]
        [InlineData(RequisitionType.Termination)]
        public void CanMove_RejectsEveryOtherMove(RequisitionType type)
        {
            var table = type == RequisitionType.Hire ? HireMoves : TerminationMoves;
            var allowed = table.Select(row => ((RequisitionStatus)row[0], (RequisitionStatus)row[1])).ToHashSet();
            foreach (var from in Enum.GetValues<RequisitionStatus>())
            foreach (var to in Enum.GetValues<RequisitionStatus>())
            {
                if (!allowed.Contains((from, to)))
                    Assert.False(RequisitionWorkflow.CanMove(type, from, to), $"{type}: {from} → {to} should not be allowed");
            }
        }

        [Fact]
        public void Termination_SkipsTheBudgetStep()
        {
            Assert.False(RequisitionWorkflow.CanMove(RequisitionType.Termination, RequisitionStatus.HRReview, RequisitionStatus.AwaitingBudget));
            Assert.True(RequisitionWorkflow.CanMove(RequisitionType.Hire, RequisitionStatus.HRReview, RequisitionStatus.AwaitingBudget));
        }

        [Theory]
        [InlineData(RequisitionStatus.Rejected)]
        [InlineData(RequisitionStatus.Closed)]
        public void FinalStatuses_HaveNoNextStatus(RequisitionStatus final)
        {
            Assert.True(RequisitionWorkflow.IsFinal(final));
            Assert.Empty(RequisitionWorkflow.NextStatuses(RequisitionType.Hire, final));
            Assert.Empty(RequisitionWorkflow.NextStatuses(RequisitionType.Termination, final));
        }

        [Fact]
        public void NextStatuses_ListsTheAllowedMovesFromReceived()
        {
            Assert.Equal(
                [RequisitionStatus.HRReview, RequisitionStatus.Rejected, RequisitionStatus.Closed],
                RequisitionWorkflow.NextStatuses(RequisitionType.Hire, RequisitionStatus.Received));
        }

        [Fact]
        public void NextStatuses_IsDefinedForEveryStatusAndType()
        {
            foreach (var type in Enum.GetValues<RequisitionType>())
            foreach (var status in Enum.GetValues<RequisitionStatus>())
                Assert.NotNull(RequisitionWorkflow.NextStatuses(type, status));
        }
    }
}
