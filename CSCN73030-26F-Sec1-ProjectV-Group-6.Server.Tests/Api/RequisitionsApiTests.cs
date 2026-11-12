/**
 * @file RequisitionsApiTests.cs
 * @brief Regression tests for /requisitions (business rule 3: Idempotency-Key) and the status workflow.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Hire requisitions. Write tests raise their own HRS requisitions with fresh keys.</summary>
    public class RequisitionsApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        private static string NewKey() => Guid.NewGuid().ToString();

        private static object HireBody(int positions = 2, string role = "Recruiter", string? reason = "GROWTH") =>
            new { departmentCode = "HRS", roleTitle = role, positions, reasonCode = reason };

        private async Task<RequisitionDto> RaiseAsync(int positions = 2) =>
            await ReadAsync<RequisitionDto>(await PostAsync("/requisitions", HireBody(positions), NewKey()), HttpStatusCode.Created);

        private async Task<RequisitionDto> MoveAsync(string number, object body) =>
            await ReadAsync<RequisitionDto>(await PatchAsync($"/requisitions/{number}", body));

        // ---- POST /requisitions -----------------------------------------------------------------------------

        [Fact]
        public async Task Create_NewKey_Returns201AsReceived()
        {
            var response = await PostAsync("/requisitions", HireBody(), NewKey());
            var requisition = await ReadAsync<RequisitionDto>(response, HttpStatusCode.Created);

            Assert.Matches(@"^REQ-\d{7}$", requisition.RequisitionNumber);
            Assert.Equal($"/requisitions/{requisition.RequisitionNumber}", response.Headers.Location?.AbsolutePath);
            Assert.Equal(RequisitionType.Hire, requisition.Type);
            Assert.Equal(RequisitionStatus.Received, requisition.Status);
            Assert.Null(requisition.BudgetDecision);
            Assert.Equal(0, requisition.PositionsFilled);
            Assert.Equal("HRS", requisition.Department.Code);
            Assert.Equal("GROWTH", requisition.ReasonCode);
            Assert.Equal(StaffHrApiFactory.Now.UtcDateTime, requisition.CreatedAtUtc);
            Assert.False(response.Headers.Contains("Idempotent-Replayed"));
        }

        [Fact]
        public async Task Create_ReplayedKeyWithSameBody_Returns200WithTheOriginal()
        {
            var key = NewKey();
            var first = await ReadAsync<RequisitionDto>(await PostAsync("/requisitions", HireBody(), key), HttpStatusCode.Created);

            var replay = await PostAsync("/requisitions", HireBody(), key);
            var second = await ReadAsync<RequisitionDto>(replay, HttpStatusCode.OK);

            Assert.Equal(first, second);
            Assert.Equal("true", replay.Headers.GetValues("Idempotent-Replayed").Single());
        }

        [Fact]
        public async Task Create_ReplayIgnoresCaseOfTheDepartmentCode()
        {
            var key = NewKey();
            await ReadAsync<RequisitionDto>(await PostAsync("/requisitions", HireBody(), key), HttpStatusCode.Created);

            var replay = await PostAsync("/requisitions", new { departmentCode = "hrs", roleTitle = "Recruiter", positions = 2, reasonCode = "GROWTH" }, key);
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        }

        [Fact]
        public async Task Create_ReplayedKey_DoesNotCreateASecondRequisition()
        {
            var key = NewKey();
            var before = (await GetAsync<List<RequisitionDto>>("/requisitions?department=HRS")).Count;

            await PostAsync("/requisitions", HireBody(), key);
            await PostAsync("/requisitions", HireBody(), key);
            await PostAsync("/requisitions", HireBody(), key);

            var after = (await GetAsync<List<RequisitionDto>>("/requisitions?department=HRS")).Count;
            Assert.Equal(before + 1, after);
        }

        [Fact]
        public async Task Create_SameKeyAtTheSameTime_CreatesOnlyOne()
        {
            var key = NewKey();

            var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => PostAsync("/requisitions", HireBody(role: "Concurrent"), key)));
            var bodies = await Task.WhenAll(responses.Select(r => ReadAsync<RequisitionDto>(r, r.StatusCode)));

            Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.OK }));
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
            Assert.Single(bodies.Select(b => b.RequisitionNumber).Distinct());
        }

        [Fact]
        public async Task Create_ReplayedKeyWithDifferentBody_Returns409()
        {
            var key = NewKey();
            var first = await ReadAsync<RequisitionDto>(await PostAsync("/requisitions", HireBody(positions: 2), key), HttpStatusCode.Created);

            var problem = await AssertProblemAsync(await PostAsync("/requisitions", HireBody(positions: 5), key), HttpStatusCode.Conflict);

            Assert.Contains(first.RequisitionNumber, problem.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Create_SeededKeyIsAlsoProtected()
        {
            await AssertProblemAsync(await PostAsync("/requisitions", HireBody(), "seed-requisition-001"), HttpStatusCode.Conflict);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_MissingOrBlankIdempotencyKey_Returns400(string? key)
        {
            var problem = await AssertProblemAsync(await PostAsync("/requisitions", HireBody(), key), HttpStatusCode.BadRequest);
            Assert.Contains("Idempotency-Key", problem.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Create_IdempotencyKeyOver100Characters_Returns400()
        {
            await AssertProblemAsync(await PostAsync("/requisitions", HireBody(), new string('k', 101)), HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_Termination_Returns422()
        {
            await AssertProblemAsync(
                await PostAsync("/requisitions", new { type = "Termination", departmentCode = "HRS", roleTitle = "X", positions = 1 }, NewKey()),
                HttpStatusCode.UnprocessableEntity, "type");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task Create_PositionsOutOfRange_Returns422(int positions)
        {
            await AssertProblemAsync(await PostAsync("/requisitions", HireBody(positions), NewKey()),
                HttpStatusCode.UnprocessableEntity, "positions");
        }

        [Fact]
        public async Task Create_MissingFieldsOrUnknownDepartment_Returns422()
        {
            await AssertProblemAsync(await PostAsync("/requisitions", new { }, NewKey()),
                HttpStatusCode.UnprocessableEntity, "departmentCode", "roleTitle", "positions");
            await AssertProblemAsync(await PostAsync("/requisitions", new { departmentCode = "XYZ", roleTitle = "X", positions = 1 }, NewKey()),
                HttpStatusCode.UnprocessableEntity, "departmentCode");
        }

        [Fact]
        public async Task Create_InvalidRequest_DoesNotUseUpTheKey()
        {
            var key = NewKey();
            await PostAsync("/requisitions", new { departmentCode = "XYZ", roleTitle = "X", positions = 1 }, key);

            await ReadAsync<RequisitionDto>(await PostAsync("/requisitions", HireBody(), key), HttpStatusCode.Created);
        }

        // ---- GET /requisitions and /requisitions/{number} ---------------------------------------------------

        [Fact]
        public async Task List_FiltersByTypeStatusAndDepartment()
        {
            var result = await GetAsync<List<RequisitionDto>>("/requisitions?type=Hire&status=Filling&department=ems");

            var filling = Assert.Single(result);
            Assert.Equal(Seed.FillingRequisition, filling.RequisitionNumber);
            Assert.Equal(3, filling.Positions);
            Assert.Equal(1, filling.PositionsFilled);
            Assert.Equal(BudgetDecision.Approved, filling.BudgetDecision);
        }

        [Fact]
        public async Task List_SeedHasOneRequisitionInEachOfSixStatuses()
        {
            var seeded = (await GetAsync<List<RequisitionDto>>("/requisitions"))
                .Where(r => r.Department.Code != "HRS").ToList();

            Assert.Equal(6, seeded.Count);
            Assert.Equal(
                [RequisitionStatus.Received, RequisitionStatus.HRReview, RequisitionStatus.AwaitingBudget,
                 RequisitionStatus.Approved, RequisitionStatus.Rejected, RequisitionStatus.Filling],
                seeded.Select(r => r.Status).Order());
        }

        [Fact]
        public async Task List_IsNewestFirst()
        {
            var all = await GetAsync<List<RequisitionDto>>("/requisitions");
            Assert.Equal(all.OrderByDescending(r => r.CreatedAtUtc).Select(r => r.RequisitionNumber), all.Select(r => r.RequisitionNumber));
        }

        [Fact]
        public async Task List_UnknownDepartment_Returns422()
        {
            await AssertProblemAsync(await Client.GetAsync("/requisitions?department=XYZ"), HttpStatusCode.UnprocessableEntity, "department");
        }

        [Fact]
        public async Task List_UnknownStatus_Returns400()
        {
            await AssertProblemAsync(await Client.GetAsync("/requisitions?status=Pending"), HttpStatusCode.BadRequest, "status");
        }

        [Fact]
        public async Task Get_ByNumberInAnyCase()
        {
            var requisition = await GetAsync<RequisitionDto>("/requisitions/req-0000001");
            Assert.Equal(Seed.FillingRequisition, requisition.RequisitionNumber);
            Assert.Equal("Primary Care Paramedic", requisition.RoleTitle);
        }

        [Fact]
        public async Task Get_UnknownNumber_Returns404()
        {
            await AssertProblemAsync(await Client.GetAsync("/requisitions/REQ-9999999"), HttpStatusCode.NotFound);
        }

        // ---- PATCH /requisitions/{number} -------------------------------------------------------------------

        [Fact]
        public async Task Patch_FollowsTheWholeWorkflowToClosed()
        {
            var r = await RaiseAsync(positions: 2);

            r = await MoveAsync(r.RequisitionNumber, new { status = "HRReview" });
            Assert.Equal(RequisitionStatus.HRReview, r.Status);
            r = await MoveAsync(r.RequisitionNumber, new { status = "AwaitingBudget", budgetDecision = "Pending" });
            Assert.Equal(BudgetDecision.Pending, r.BudgetDecision);
            r = await MoveAsync(r.RequisitionNumber, new { status = "Approved", budgetDecision = "Approved" });
            Assert.Equal(RequisitionStatus.Approved, r.Status);
            r = await MoveAsync(r.RequisitionNumber, new { status = "Filling", positionsFilled = 1 });
            Assert.Equal(1, r.PositionsFilled);
            r = await MoveAsync(r.RequisitionNumber, new { status = "Completed", positionsFilled = 2 });
            Assert.Equal(RequisitionStatus.Completed, r.Status);
            r = await MoveAsync(r.RequisitionNumber, new { status = "Closed" });
            Assert.Equal(RequisitionStatus.Closed, r.Status);

            Assert.Equal(r, await GetAsync<RequisitionDto>($"/requisitions/{r.RequisitionNumber}"));
        }

        [Fact]
        public async Task Patch_SkippingAStep_Returns422ListingAllowedMoves()
        {
            var r = await RaiseAsync();

            var problem = await AssertProblemAsync(await PatchAsync($"/requisitions/{r.RequisitionNumber}", new { status = "Filling" }),
                HttpStatusCode.UnprocessableEntity, "status");

            var message = problem.GetProperty("errors").GetProperty("status")[0].GetString();
            Assert.Contains("HRReview, Rejected, Closed", message);
        }

        [Fact]
        public async Task Patch_ApprovedWithoutBudgetApproval_Returns422()
        {
            var r = await RaiseAsync();
            await MoveAsync(r.RequisitionNumber, new { status = "HRReview" });
            await MoveAsync(r.RequisitionNumber, new { status = "AwaitingBudget", budgetDecision = "Pending" });

            await AssertProblemAsync(await PatchAsync($"/requisitions/{r.RequisitionNumber}", new { status = "Approved" }),
                HttpStatusCode.UnprocessableEntity, "budgetDecision");

            // Budget's approval recorded first, then Approved on its own, also works.
            await MoveAsync(r.RequisitionNumber, new { budgetDecision = "Approved" });
            Assert.Equal(RequisitionStatus.Approved, (await MoveAsync(r.RequisitionNumber, new { status = "Approved" })).Status);
        }

        [Fact]
        public async Task Patch_CompletedBeforeEveryPositionIsFilled_Returns422()
        {
            var r = await RaiseAsync(positions: 3);
            await MoveAsync(r.RequisitionNumber, new { status = "HRReview" });
            await MoveAsync(r.RequisitionNumber, new { status = "AwaitingBudget" });
            await MoveAsync(r.RequisitionNumber, new { status = "Approved", budgetDecision = "Approved" });
            await MoveAsync(r.RequisitionNumber, new { status = "Filling", positionsFilled = 2 });

            await AssertProblemAsync(await PatchAsync($"/requisitions/{r.RequisitionNumber}", new { status = "Completed" }),
                HttpStatusCode.UnprocessableEntity, "positionsFilled");
        }

        [Theory]
        [InlineData("Rejected")]
        [InlineData("Closed")]
        public async Task Patch_FinalStatus_CannotChange(string final)
        {
            var r = await RaiseAsync();
            await MoveAsync(r.RequisitionNumber, new { status = final });

            var problem = await AssertProblemAsync(await PatchAsync($"/requisitions/{r.RequisitionNumber}", new { status = "HRReview" }),
                HttpStatusCode.UnprocessableEntity, "status");
            Assert.Contains("final", problem.GetProperty("errors").GetProperty("status")[0].GetString());
        }

        [Fact]
        public async Task Patch_SameStatusAgain_IsAccepted()
        {
            var r = await RaiseAsync();
            Assert.Equal(RequisitionStatus.Received, (await MoveAsync(r.RequisitionNumber, new { status = "Received" })).Status);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public async Task Patch_PositionsFilledOutOfRange_Returns422(int filled)
        {
            var r = await RaiseAsync(positions: 2);
            await AssertProblemAsync(await PatchAsync($"/requisitions/{r.RequisitionNumber}", new { positionsFilled = filled }),
                HttpStatusCode.UnprocessableEntity, "positionsFilled");
        }

        [Fact]
        public async Task Patch_EmptyBody_Returns422()
        {
            var r = await RaiseAsync();
            await AssertProblemAsync(await PatchAsync($"/requisitions/{r.RequisitionNumber}", new { }),
                HttpStatusCode.UnprocessableEntity, "body");
        }

        [Fact]
        public async Task Patch_UnknownNumber_Returns404()
        {
            await AssertProblemAsync(await PatchAsync("/requisitions/REQ-9999999", new { status = "HRReview" }), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Patch_UnknownStatusValue_Returns400()
        {
            var r = await RaiseAsync();
            await AssertProblemAsync(await PatchAsync($"/requisitions/{r.RequisitionNumber}", "{\"status\": \"Done\"}"),
                HttpStatusCode.BadRequest, "$.status");
        }

        // ---- Vacancies in the department headcount ------------------------------------------------------------

        [Fact]
        public async Task Vacancies_CountApprovedAndFillingRequisitionsOnly()
        {
            async Task<int> HrsVacanciesAsync() =>
                (await GetAsync<List<DepartmentHeadcountDto>>("/departments/headcount")).Single(d => d.Code == "HRS").Vacancies;

            var r = await RaiseAsync(positions: 4);
            var baseline = await HrsVacanciesAsync();

            await MoveAsync(r.RequisitionNumber, new { status = "HRReview" });
            await MoveAsync(r.RequisitionNumber, new { status = "AwaitingBudget" });
            Assert.Equal(baseline, await HrsVacanciesAsync());          // not funded yet

            await MoveAsync(r.RequisitionNumber, new { status = "Approved", budgetDecision = "Approved" });
            Assert.Equal(baseline + 4, await HrsVacanciesAsync());      // 4 open positions

            await MoveAsync(r.RequisitionNumber, new { status = "Filling", positionsFilled = 3 });
            Assert.Equal(baseline + 1, await HrsVacanciesAsync());      // 1 still open

            await MoveAsync(r.RequisitionNumber, new { status = "Closed" });
            Assert.Equal(baseline, await HrsVacanciesAsync());
        }
    }
}
