/**
 * @file TerminationApiTests.cs
 * @brief Regression tests for termination requisitions and what completing one does.
 */

using System.Net;
using System.Text.Json;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>
    /// Termination requisitions: Received → HRReview → Approved → Completed. Completing one terminates
    /// the employee, ends their assignments, cancels unfinished shifts and publishes Staff.Terminated.
    /// </summary>
    public class TerminationApiTests : ApiTestBase
    {
        private static readonly DateOnly Today = StaffHrApiFactory.Today;

        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public TerminationApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        private static string NewKey() => Guid.NewGuid().ToString();

        private static object TerminationBody(string staffNumber, string reason = "RESIGNATION") =>
            new { type = "Termination", staffNumber, reasonCode = reason };

        private async Task<RequisitionDto> RaiseAsync(string staffNumber) =>
            await ReadAsync<RequisitionDto>(await PostAsync("/requisitions", TerminationBody(staffNumber), NewKey()), HttpStatusCode.Created);

        private async Task<RequisitionDto> MoveAsync(string number, string status) =>
            await ReadAsync<RequisitionDto>(await PatchAsync($"/requisitions/{number}", new { status }));

        private async Task<RequisitionDto> CompleteAsync(string number)
        {
            await MoveAsync(number, "HRReview");
            await MoveAsync(number, "Approved");
            return await MoveAsync(number, "Completed");
        }

        /// <summary>An HRS employee with a current role, a future role, and a past, an in-progress and a future shift.</summary>
        private async Task<(StaffProfileDto Staff, int ServiceId)> EmployeeWithHistoryAsync()
        {
            var serviceId = await ServiceIdAsync("HRS", "Employee Relations");
            var staff = await CreateStaffAsync(new CreateAssignmentRequest(serviceId, "Advisor", Today.AddDays(-30), null));
            await ReadAsync<AssignmentDto>(await PostAsync($"/staff/{staff.StaffNumber}/assignments",
                new CreateAssignmentRequest(serviceId, "Senior Advisor", Today.AddDays(10), null)), HttpStatusCode.Created);

            foreach (var (start, end) in new[] { ("2026-11-04T08:00:00Z", "2026-11-04T16:00:00Z"),
                                                  ("2026-11-05T08:00:00Z", "2026-11-05T16:00:00Z"),
                                                  ("2026-11-20T08:00:00Z", "2026-11-20T16:00:00Z") })
            {
                await ReadAsync<RosterEntryDto>(await PostAsync("/rosters/entries",
                    new { staffNumber = staff.StaffNumber, serviceId, shiftStart = start, shiftEnd = end, shiftType = "Day" }),
                    HttpStatusCode.Created);
            }
            return (staff, serviceId);
        }

        // ---- Raising one ---------------------------------------------------------------------------------------

        [Fact]
        public async Task Create_ReturnsATerminationForTheEmployee()
        {
            var (staff, _) = await EmployeeWithHistoryAsync();

            var requisition = await RaiseAsync(staff.StaffNumber);

            Assert.Equal(RequisitionType.Termination, requisition.Type);
            Assert.Equal(staff.StaffNumber, requisition.StaffNumber);
            Assert.Equal(RequisitionStatus.Received, requisition.Status);
            Assert.Equal("HRS", requisition.Department.Code);   // defaults to the home department
            Assert.Equal("Advisor", requisition.RoleTitle);      // defaults to the current role
            Assert.Equal(1, requisition.Positions);
            Assert.Equal("RESIGNATION", requisition.ReasonCode);
            Assert.Null(requisition.BudgetDecision);
        }

        [Fact]
        public async Task Create_ReplayWithSameKey_ReturnsTheOriginal_DifferentEmployee_Is409()
        {
            var first = await CreateStaffAsync();
            var second = await CreateStaffAsync();
            var key = NewKey();
            var original = await ReadAsync<RequisitionDto>(await PostAsync("/requisitions", TerminationBody(first.StaffNumber), key), HttpStatusCode.Created);

            var replay = await PostAsync("/requisitions", TerminationBody(first.StaffNumber), key);
            Assert.Equal(original, await ReadAsync<RequisitionDto>(replay));

            await AssertProblemAsync(await PostAsync("/requisitions", TerminationBody(second.StaffNumber), key), HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_SecondOpenTerminationForTheSamePerson_Returns409()
        {
            var staff = await CreateStaffAsync();
            await RaiseAsync(staff.StaffNumber);

            await AssertProblemAsync(await PostAsync("/requisitions", TerminationBody(staff.StaffNumber), NewKey()), HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_AfterARejectedTermination_IsAllowed()
        {
            var staff = await CreateStaffAsync();
            var rejected = await RaiseAsync(staff.StaffNumber);
            await MoveAsync(rejected.RequisitionNumber, "Rejected");

            await RaiseAsync(staff.StaffNumber);
        }

        [Fact]
        public async Task Create_ForAnAlreadyTerminatedEmployee_Returns422()
        {
            await AssertProblemAsync(await PostAsync("/requisitions", TerminationBody(Seed.Matteo), NewKey()),
                HttpStatusCode.UnprocessableEntity, "staffNumber");
        }

        [Fact]
        public async Task Create_InvalidTermination_Returns422PerField()
        {
            await AssertProblemAsync(
                await PostAsync("/requisitions", new { type = "Termination", staffNumber = "STF-9999999", positions = 2, departmentCode = "XYZ" }, NewKey()),
                HttpStatusCode.UnprocessableEntity, "staffNumber", "reasonCode", "positions", "departmentCode");
        }

        [Fact]
        public async Task Create_HireNamingAnEmployee_Returns422()
        {
            await AssertProblemAsync(
                await PostAsync("/requisitions", new { departmentCode = "HRS", roleTitle = "X", positions = 1, staffNumber = Seed.Talia }, NewKey()),
                HttpStatusCode.UnprocessableEntity, "staffNumber");
        }

        // ---- Workflow ------------------------------------------------------------------------------------------

        [Fact]
        public async Task Workflow_SkippingHRReview_Returns422()
        {
            var requisition = await RaiseAsync((await CreateStaffAsync()).StaffNumber);
            await AssertProblemAsync(await PatchAsync($"/requisitions/{requisition.RequisitionNumber}", new { status = "Approved" }),
                HttpStatusCode.UnprocessableEntity, "status");
        }

        [Fact]
        public async Task Workflow_HasNoBudgetStepOrPositionsToFill()
        {
            var requisition = await RaiseAsync((await CreateStaffAsync()).StaffNumber);
            await MoveAsync(requisition.RequisitionNumber, "HRReview");

            await AssertProblemAsync(await PatchAsync($"/requisitions/{requisition.RequisitionNumber}", new { status = "AwaitingBudget" }),
                HttpStatusCode.UnprocessableEntity, "status");
            await AssertProblemAsync(await PatchAsync($"/requisitions/{requisition.RequisitionNumber}", new { budgetDecision = "Approved" }),
                HttpStatusCode.UnprocessableEntity, "budgetDecision");
            await AssertProblemAsync(await PatchAsync($"/requisitions/{requisition.RequisitionNumber}", new { positionsFilled = 1 }),
                HttpStatusCode.UnprocessableEntity, "positionsFilled");
        }

        [Fact]
        public async Task Workflow_ApprovedIsNotYetATermination()
        {
            var staff = await CreateStaffAsync();
            var requisition = await RaiseAsync(staff.StaffNumber);
            await MoveAsync(requisition.RequisitionNumber, "HRReview");
            await MoveAsync(requisition.RequisitionNumber, "Approved");

            Assert.Equal(EmploymentStatus.Probation, (await GetAsync<StaffProfileDto>($"/staff/{staff.StaffNumber}")).EmploymentStatus);
        }

        [Fact]
        public async Task Workflow_RejectedLeavesTheEmployeeUntouched()
        {
            var (staff, _) = await EmployeeWithHistoryAsync();
            var requisition = await RaiseAsync(staff.StaffNumber);
            await MoveAsync(requisition.RequisitionNumber, "Rejected");

            var profile = await GetAsync<StaffProfileDto>($"/staff/{staff.StaffNumber}");
            Assert.Equal(EmploymentStatus.Probation, profile.EmploymentStatus);
            Assert.Equal(2, profile.Assignments.Count);
        }

        // ---- Completing one ------------------------------------------------------------------------------------

        [Fact]
        public async Task Complete_TerminatesTheEmployeeAndKeepsTheStaffNumber()
        {
            var (staff, _) = await EmployeeWithHistoryAsync();
            var requisition = await CompleteAsync((await RaiseAsync(staff.StaffNumber)).RequisitionNumber);

            Assert.Equal(RequisitionStatus.Completed, requisition.Status);
            Assert.Equal(1, requisition.PositionsFilled);
            var profile = await GetAsync<StaffProfileDto>($"/staff/{staff.StaffNumber}");
            Assert.Equal(EmploymentStatus.Terminated, profile.EmploymentStatus);
            Assert.Equal(staff.StaffNumber, profile.StaffNumber);
        }

        [Fact]
        public async Task Complete_EndsCurrentAssignmentsTodayAndRemovesFutureOnes()
        {
            var (staff, _) = await EmployeeWithHistoryAsync();
            await CompleteAsync((await RaiseAsync(staff.StaffNumber)).RequisitionNumber);

            var assignment = Assert.Single(await GetAsync<List<AssignmentDto>>($"/staff/{staff.StaffNumber}/assignments"));
            Assert.Equal("Advisor", assignment.RoleTitle);
            Assert.Equal(Today, assignment.EndDate);
        }

        [Fact]
        public async Task Complete_CancelsUnfinishedShiftsOnly()
        {
            var (staff, serviceId) = await EmployeeWithHistoryAsync();
            await CompleteAsync((await RaiseAsync(staff.StaffNumber)).RequisitionNumber);

            var shifts = (await GetAsync<List<RosterEntryDto>>($"/rosters?service={serviceId}&from=2026-11-01&to=2026-12-01"))
                .Where(e => e.StaffNumber == staff.StaffNumber)
                .OrderBy(e => e.ShiftStart)
                .Select(e => e.Status)
                .ToList();

            // Past shift kept; the one in progress at 12:00 and the future one cancelled.
            Assert.Equal([RosterStatus.Scheduled, RosterStatus.Cancelled, RosterStatus.Cancelled], shifts);
        }

        [Fact]
        public async Task Complete_RemovesDutyEligibility()
        {
            var (staff, _) = await EmployeeWithHistoryAsync();
            Assert.True((await GetAsync<DutyEligibilityDto>($"/staff/{staff.StaffNumber}/eligibility")).Eligible);

            await CompleteAsync((await RaiseAsync(staff.StaffNumber)).RequisitionNumber);

            var eligibility = await GetAsync<DutyEligibilityDto>($"/staff/{staff.StaffNumber}/eligibility");
            Assert.False(eligibility.Eligible);
            Assert.Contains(eligibility.Reasons, r => r.Code == "EMPLOYMENT_STATUS");
        }

        [Fact]
        public async Task Complete_PublishesStaffTerminated()
        {
            var (staff, _) = await EmployeeWithHistoryAsync();
            var requisition = await CompleteAsync((await RaiseAsync(staff.StaffNumber)).RequisitionNumber);

            var terminated = Assert.Single(await OutboxEventsForAsync(staff.StaffNumber), e => e.EventType == IntegrationEventTypes.StaffTerminated);
            var payload = JsonDocument.Parse(terminated.PayloadJson).RootElement;
            Assert.Equal(staff.StaffNumber, payload.GetProperty("staffNumber").GetString());
            Assert.Equal("HRS", payload.GetProperty("departmentCode").GetString());
            Assert.Equal("2026-11-05", payload.GetProperty("effectiveDate").GetString());
            Assert.Equal(requisition.RequisitionNumber, payload.GetProperty("requisitionNumber").GetString());
            Assert.Equal(2, payload.GetProperty("cancelledShifts").GetInt32());
        }

        [Fact]
        public async Task Complete_IsAudited()
        {
            var staff = await CreateStaffAsync();
            await CompleteAsync((await RaiseAsync(staff.StaffNumber)).RequisitionNumber);

            var audit = await GetAsync<PagedResult<AuditLogDto>>($"/audit?entityType=Staff&entityId={staff.StaffNumber}");
            Assert.Equal(["Updated", "Created"], audit.Items.Select(a => a.Action));
        }

        [Fact]
        public async Task Complete_ThenClosed_IsAllowed_ButNotReopened()
        {
            var requisition = await CompleteAsync((await RaiseAsync((await CreateStaffAsync()).StaffNumber)).RequisitionNumber);

            await MoveAsync(requisition.RequisitionNumber, "Closed");
            await AssertProblemAsync(await PatchAsync($"/requisitions/{requisition.RequisitionNumber}", new { status = "HRReview" }),
                HttpStatusCode.UnprocessableEntity, "status");
        }

        [Fact]
        public async Task Terminations_AreNotVacancies_AndFilterByType()
        {
            var before = (await GetAsync<List<DepartmentHeadcountDto>>("/departments/headcount")).Single(d => d.Code == "HRS").Vacancies;
            var requisition = await CompleteAsync((await RaiseAsync((await CreateStaffAsync()).StaffNumber)).RequisitionNumber);

            var after = (await GetAsync<List<DepartmentHeadcountDto>>("/departments/headcount")).Single(d => d.Code == "HRS").Vacancies;
            Assert.Equal(before, after);

            var terminations = await GetAsync<List<RequisitionDto>>("/requisitions?type=Termination");
            Assert.Contains(terminations, r => r.RequisitionNumber == requisition.RequisitionNumber);
            Assert.All(terminations, r => Assert.Equal(RequisitionType.Termination, r.Type));
        }
    }
}
