/**
 * @file BudgetFundingApiTests.cs
 * @brief Regression tests for sending hire requisitions to Budget and reading Budget's decisions back.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>POST /requisitions/{number}/funding-request and .../sync, against the fake Finance API.</summary>
    public class BudgetFundingApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        /// <summary>Raises a hire requisition and moves it to HRReview, ready for Budget.</summary>
        private async Task<RequisitionDto> InReviewAsync(string department = "EMS", string role = "Primary Care Paramedic", int positions = 2)
        {
            var raised = await ReadAsync<RequisitionDto>(await PostAsync("/requisitions",
                new { departmentCode = department, roleTitle = role, positions, reasonCode = "VACANCY" }, Guid.NewGuid().ToString()),
                HttpStatusCode.Created);
            return await ReadAsync<RequisitionDto>(await PatchAsync($"/requisitions/{raised.RequisitionNumber}", new { status = "HRReview" }));
        }

        private Task<HttpResponseMessage> SendToBudgetAsync(string number, object? body = null) =>
            PostAsync($"/requisitions/{number}/funding-request", body ?? new { });

        private (string Call, System.Text.Json.Nodes.JsonNode? Body) LastBudgetCall() => Factory.Budget.Calls.Last();

        [Fact]
        public async Task Send_CreatesAFundingRequestAndWaitsForBudget()
        {
            var requisition = await InReviewAsync();

            var sent = await ReadAsync<RequisitionDto>(await SendToBudgetAsync(requisition.RequisitionNumber));

            Assert.Equal(RequisitionStatus.AwaitingBudget, sent.Status);
            Assert.Equal(BudgetDecision.Pending, sent.BudgetDecision);
            Assert.NotNull(sent.Funding);
            Assert.True(sent.Funding!.Amount > 0, "The cost is estimated from current paramedic salaries.");
            Assert.False(sent.Funding.Urgent);
            Assert.Equal(StaffHrApiFactory.Now.UtcDateTime, sent.Funding.RequestedAtUtc);
            Assert.Null(sent.Funding.DecidedAtUtc);

            var (call, body) = LastBudgetCall();
            Assert.Equal("POST /api/budget/v1/funding-requests", call);
            Assert.Equal("EMS", body!["departmentCode"]!.GetValue<string>());
            Assert.Equal(sent.Funding.Amount, body["amount"]!.GetValue<decimal>());
            Assert.StartsWith($"{requisition.RequisitionNumber}: hire 2 x Primary Care Paramedic (EMS)", body["reason"]!.GetValue<string>());
            Assert.Contains(Factory.Budget.FundingRequests, f => f["id"]!.GetValue<string>() == sent.Funding.RequestId.ToString());
        }

        [Fact]
        public async Task Send_WithACostPerPosition_SendsPositionsTimesCostUnderBudgetsDepartmentCode()
        {
            var requisition = await InReviewAsync(department: "PTR", role: "Bus Operator", positions: 3);

            var sent = await ReadAsync<RequisitionDto>(await SendToBudgetAsync(requisition.RequisitionNumber,
                new { annualCostPerPosition = 61_500.50m, urgent = true }));

            Assert.Equal(184_501.50m, sent.Funding!.Amount);
            Assert.True(sent.Funding.Urgent);
            var body = LastBudgetCall().Body!;
            Assert.Equal("TRANSIT", body["departmentCode"]!.GetValue<string>());
            Assert.True(body["urgent"]!.GetValue<bool>());
        }

        [Fact]
        public async Task Sync_AppliesAnApprovalAndTheHireCanBeFilled()
        {
            var requisition = await InReviewAsync();
            var sent = await ReadAsync<RequisitionDto>(await SendToBudgetAsync(requisition.RequisitionNumber));

            var stillPending = await ReadAsync<RequisitionDto>(await PostAsync($"/requisitions/{sent.RequisitionNumber}/funding-request/sync", new { }));
            Assert.Equal(RequisitionStatus.AwaitingBudget, stillPending.Status);
            Assert.NotNull(stillPending.Funding!.LastCheckedAtUtc);

            Factory.Budget.Decide(sent.Funding!.RequestId, approved: true);
            var approved = await ReadAsync<RequisitionDto>(await PostAsync($"/requisitions/{sent.RequisitionNumber}/funding-request/sync", new { }));

            Assert.Equal(RequisitionStatus.Approved, approved.Status);
            Assert.Equal(BudgetDecision.Approved, approved.BudgetDecision);
            Assert.Equal(new DateTime(2026, 11, 6, 9, 30, 0, DateTimeKind.Utc), approved.Funding!.DecidedAtUtc);
            var filling = await ReadAsync<RequisitionDto>(await PatchAsync($"/requisitions/{sent.RequisitionNumber}", new { status = "Filling" }));
            Assert.Equal(RequisitionStatus.Filling, filling.Status);
        }

        [Fact]
        public async Task SyncAll_AppliesARejectionWithOneCallToBudget()
        {
            var requisition = await InReviewAsync();
            var sent = await ReadAsync<RequisitionDto>(await SendToBudgetAsync(requisition.RequisitionNumber));
            Factory.Budget.Decide(sent.Funding!.RequestId, approved: false);
            var callsBefore = Factory.Budget.Calls.Count;

            using (var scope = Factory.Services.CreateScope())
                Assert.True(await scope.ServiceProvider.GetRequiredService<BudgetFundingService>().SyncAllAsync() >= 1);

            Assert.Equal(1, Factory.Budget.Calls.Count - callsBefore);
            var rejected = await GetAsync<RequisitionDto>($"/requisitions/{sent.RequisitionNumber}");
            Assert.Equal(RequisitionStatus.Rejected, rejected.Status);
            Assert.Equal(BudgetDecision.Rejected, rejected.BudgetDecision);
        }

        [Fact]
        public async Task Sync_LeavesARequisitionHrAlreadyClosed()
        {
            var requisition = await InReviewAsync();
            var sent = await ReadAsync<RequisitionDto>(await SendToBudgetAsync(requisition.RequisitionNumber));
            await ReadAsync<RequisitionDto>(await PatchAsync($"/requisitions/{sent.RequisitionNumber}", new { status = "Closed" }));
            Factory.Budget.Decide(sent.Funding!.RequestId, approved: true);

            var after = await ReadAsync<RequisitionDto>(await PostAsync($"/requisitions/{sent.RequisitionNumber}/funding-request/sync", new { }));

            Assert.Equal(RequisitionStatus.Closed, after.Status);
            Assert.NotNull(after.Funding!.DecidedAtUtc);
        }

        [Fact]
        public async Task Send_TwiceIsAConflict()
        {
            var requisition = await InReviewAsync();
            await ReadAsync<RequisitionDto>(await SendToBudgetAsync(requisition.RequisitionNumber));

            await AssertProblemAsync(await SendToBudgetAsync(requisition.RequisitionNumber), HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Send_ARequisitionNotInReviewIs422()
        {
            var received = await ReadAsync<RequisitionDto>(await PostAsync("/requisitions",
                new { departmentCode = "EMS", roleTitle = "Dispatcher", positions = 1 }, Guid.NewGuid().ToString()), HttpStatusCode.Created);

            await AssertProblemAsync(await SendToBudgetAsync(received.RequisitionNumber), HttpStatusCode.UnprocessableEntity, "status");
        }

        [Fact]
        public async Task Send_ATerminationIs422()
        {
            var staff = await CreateStaffAsync();
            var termination = await ReadAsync<RequisitionDto>(await PostAsync("/requisitions",
                new { type = "Termination", staffNumber = staff.StaffNumber, reasonCode = "RESIGNATION" }, Guid.NewGuid().ToString()),
                HttpStatusCode.Created);

            await AssertProblemAsync(await SendToBudgetAsync(termination.RequisitionNumber), HttpStatusCode.UnprocessableEntity, "type");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(2_000_000)]
        public async Task Send_ABadCostIs422(decimal cost)
        {
            var requisition = await InReviewAsync();
            await AssertProblemAsync(await SendToBudgetAsync(requisition.RequisitionNumber, new { annualCostPerPosition = cost }),
                HttpStatusCode.UnprocessableEntity, "annualCostPerPosition");
        }

        [Fact]
        public async Task Send_WhenBudgetIsDown_Is502AndChangesNothing()
        {
            var requisition = await InReviewAsync();
            Factory.Budget.FailWith = HttpStatusCode.ServiceUnavailable;
            try
            {
                var problem = await AssertProblemAsync(await SendToBudgetAsync(requisition.RequisitionNumber), HttpStatusCode.BadGateway);
                Assert.Contains("Budget is down for maintenance", problem.GetProperty("detail").GetString());
            }
            finally
            {
                Factory.Budget.FailWith = null;
            }

            var unchanged = await GetAsync<RequisitionDto>($"/requisitions/{requisition.RequisitionNumber}");
            Assert.Equal(RequisitionStatus.HRReview, unchanged.Status);
            Assert.Null(unchanged.Funding);
        }

        [Fact]
        public async Task Sync_BeforeSendingIs422AndAnUnknownRequisitionIs404()
        {
            var requisition = await InReviewAsync();
            await AssertProblemAsync(await PostAsync($"/requisitions/{requisition.RequisitionNumber}/funding-request/sync", new { }),
                HttpStatusCode.UnprocessableEntity, "fundingRequest");
            await AssertProblemAsync(await SendToBudgetAsync("REQ-0999999"), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Send_NeedsTheAdminRole()
        {
            var requisition = await InReviewAsync();
            var request = new HttpRequestMessage(HttpMethod.Post, $"/requisitions/{requisition.RequisitionNumber}/funding-request")
            {
                Content = System.Net.Http.Json.JsonContent.Create(new { }),
            };
            request.Headers.Add("X-Dev-Roles", StaffHrRoles.Read);

            await AssertProblemAsync(await Client.SendAsync(request), HttpStatusCode.Forbidden);
        }
    }
}
