/**
 * @file DutyEligibilityRulesTests.cs
 * @brief Unit tests for the duty eligibility rules.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Codes = CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain.DutyEligibilityRules.Codes;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>Status, current assignment, duty-blocking credentials and security flags.</summary>
    public class DutyEligibilityRulesTests
    {
        private static readonly DateOnly Day = new(2026, 11, 5);
        private static readonly CredentialType Blocking = new() { Name = "Paramedic Licence", IsDutyBlocking = true };
        private static readonly CredentialType NotBlocking = new() { Name = "HR Certificate", IsDutyBlocking = false };

        private static Staff EligibleStaff() => new()
        {
            EmploymentStatus = EmploymentStatus.Active,
            Assignments = [new StaffAssignment { ServiceId = 1, StartDate = Day.AddYears(-1) }],
            Credentials = [new Credential { CredentialType = Blocking, Number = "P-1", ExpiryDate = Day.AddDays(200) }],
        };

        private static string[] ReasonCodes(Staff staff, IEnumerable<SecurityFlag>? flags = null, int? serviceId = null) =>
            DutyEligibilityRules.Evaluate(staff, flags ?? [], Day, serviceId).Reasons.Select(r => r.Code).ToArray();

        [Fact]
        public void ActiveStaffWithCurrentAssignmentAndValidCredentials_IsEligible()
        {
            var decision = DutyEligibilityRules.Evaluate(EligibleStaff(), [], Day, null);

            Assert.True(decision.Eligible);
            Assert.Empty(decision.Reasons);
            Assert.Empty(decision.Warnings);
        }

        [Theory]
        [InlineData(EmploymentStatus.Active, true)]
        [InlineData(EmploymentStatus.Probation, true)]
        [InlineData(EmploymentStatus.OnLeave, false)]
        [InlineData(EmploymentStatus.Suspended, false)]
        [InlineData(EmploymentStatus.Terminated, false)]
        public void EmploymentStatus_OnlyActiveAndProbationMayWork(EmploymentStatus status, bool eligible)
        {
            var staff = EligibleStaff();
            staff.EmploymentStatus = status;

            Assert.Equal(eligible, !ReasonCodes(staff).Contains(Codes.EmploymentStatus));
        }

        [Fact]
        public void NoAssignmentCoveringTheDay_IsIneligible()
        {
            var staff = EligibleStaff();
            staff.Assignments[0].EndDate = Day.AddDays(-1);

            Assert.Equal([Codes.NoCurrentAssignment], ReasonCodes(staff));
        }

        [Fact]
        public void AssignmentInAnotherService_IsIneligibleForThatService()
        {
            Assert.Empty(ReasonCodes(EligibleStaff(), serviceId: 1));
            Assert.Equal([Codes.NoCurrentAssignment], ReasonCodes(EligibleStaff(), serviceId: 2));
        }

        [Fact]
        public void ExpiredDutyBlockingCredential_IsIneligible()
        {
            var staff = EligibleStaff();
            staff.Credentials[0].ExpiryDate = Day.AddDays(-1);

            Assert.Equal([Codes.CredentialExpired], ReasonCodes(staff));
        }

        [Fact]
        public void SuspendedDutyBlockingCredential_IsIneligible()
        {
            var staff = EligibleStaff();
            staff.Credentials[0].Status = CredentialStatus.Suspended;

            Assert.Equal([Codes.CredentialSuspended], ReasonCodes(staff));
        }

        [Fact]
        public void ExpiringDutyBlockingCredential_IsOnlyAWarning()
        {
            var staff = EligibleStaff();
            staff.Credentials[0].ExpiryDate = Day.AddDays(10);

            var decision = DutyEligibilityRules.Evaluate(staff, [], Day, null);

            Assert.True(decision.Eligible);
            Assert.Equal(Codes.CredentialExpiring, Assert.Single(decision.Warnings).Code);
        }

        [Fact]
        public void ExpiredCredentialThatDoesNotBlockDuty_IsIgnored()
        {
            var staff = EligibleStaff();
            staff.Credentials.Add(new Credential { CredentialType = NotBlocking, Number = "H-1", ExpiryDate = Day.AddDays(-100) });

            Assert.True(DutyEligibilityRules.Evaluate(staff, [], Day, null).Eligible);
        }

        [Fact]
        public void ActiveSecurityFlag_IsIneligible_ClearedIsNot()
        {
            SecurityFlag[] flags =
            [
                new() { FlagType = "UnderInvestigation", Status = SecurityFlagStatus.Active },
                new() { FlagType = "OldCheck", Status = SecurityFlagStatus.Cleared },
            ];

            var reasons = DutyEligibilityRules.Evaluate(EligibleStaff(), flags, Day, null).Reasons;

            var reason = Assert.Single(reasons);
            Assert.Equal(Codes.SecurityFlagActive, reason.Code);
            Assert.Contains("UnderInvestigation", reason.Message);
        }

        [Fact]
        public void EveryProblemIsReported()
        {
            var staff = EligibleStaff();
            staff.EmploymentStatus = EmploymentStatus.Suspended;
            staff.Assignments.Clear();
            staff.Credentials[0].ExpiryDate = Day.AddDays(-5);
            staff.Credentials.Add(new Credential { CredentialType = Blocking, Number = "P-2", ExpiryDate = Day.AddDays(300), Status = CredentialStatus.Suspended });

            var codes = ReasonCodes(staff, [new SecurityFlag { FlagType = "X", Status = SecurityFlagStatus.Active }]);

            Assert.Equal([Codes.EmploymentStatus, Codes.NoCurrentAssignment, Codes.CredentialExpired, Codes.CredentialSuspended, Codes.SecurityFlagActive], codes);
        }
    }
}
