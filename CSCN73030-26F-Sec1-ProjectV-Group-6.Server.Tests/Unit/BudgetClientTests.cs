/**
 * @file BudgetClientTests.cs
 * @brief Unit tests for the Budget (Finance API) client: requests sent, answers read, failures reported.
 */

using System.Net;
using System.Text.Json;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Budget;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>BudgetClient talks to the Finance API in its own shapes and turns failures into 502/503.</summary>
    public class BudgetClientTests
    {
        private static BudgetClient Client(FakeBudgetApi fake, string? baseUrl = FakeBudgetApi.BaseUrl) =>
            new(new HttpClient(fake) { BaseAddress = baseUrl == null ? null : new Uri(baseUrl) },
                Options.Create(new BudgetOptions { BaseUrl = baseUrl }), NullLogger<BudgetClient>.Instance);

        [Fact]
        public async Task CreateFundingRequest_PostsTheBodyAndReadsTheId()
        {
            var fake = new FakeBudgetApi();
            var created = await Client(fake).CreateFundingRequestAsync("EMS", 178_000m, "REQ-0000007", urgent: true);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal(FundingStatus.Pending, created.Status);
            Assert.Equal(178_000m, created.Amount);
            var (call, body) = Assert.Single(fake.Calls);
            Assert.Equal("POST /api/budget/v1/funding-requests", call);
            Assert.Equal("EMS", body!["departmentCode"]!.GetValue<string>());
            Assert.True(body["urgent"]!.GetValue<bool>());
        }

        [Fact]
        public async Task ListFundingRequests_ReadsBudgetsDecision()
        {
            var fake = new FakeBudgetApi();
            var client = Client(fake);
            var approved = await client.CreateFundingRequestAsync("EMS", 1m, "a", false);
            var rejected = await client.CreateFundingRequestAsync("SEC", 1m, "b", false);
            fake.Decide(approved.Id, approved: true);
            fake.Decide(rejected.Id, approved: false);

            var list = await client.ListFundingRequestsAsync();

            Assert.Equal(FundingStatus.Approved, list.Single(r => r.Id == approved.Id).Status);
            Assert.Equal(FundingStatus.Rejected, list.Single(r => r.Id == rejected.Id).Status);
            Assert.NotNull(list.Single(r => r.Id == approved.Id).DecidedAtUtc);
        }

        [Theory]
        [InlineData("0", FundingStatus.Pending)]
        [InlineData("1", FundingStatus.Approved)]
        [InlineData("2", FundingStatus.Rejected)]
        [InlineData("\"Approved\"", FundingStatus.Approved)]
        [InlineData("\"rejected\"", FundingStatus.Rejected)]
        [InlineData("null", FundingStatus.Pending)]
        public void ParseStatus_AcceptsNumbersAndNames(string json, FundingStatus expected) =>
            Assert.Equal(expected, BudgetClient.ParseStatus(JsonDocument.Parse(json).RootElement));

        [Fact]
        public async Task GetDepartmentBudget_ReadsTheFiguresOrNullForAnUnknownDepartment()
        {
            var fake = new FakeBudgetApi();
            fake.SetSpent("HR", 200_000m);
            var client = Client(fake);

            var budget = await client.GetDepartmentBudgetAsync("HR");
            Assert.Equal(1_200_000m, budget!.Allocated);
            Assert.Equal(1_000_000m, budget.Remaining);
            Assert.Null(await client.GetDepartmentBudgetAsync("NOPE"));
        }

        [Fact]
        public async Task CreateExpense_ReportsAcceptedAndRefusedExpenses()
        {
            var client = Client(new FakeBudgetApi());

            var accepted = await client.CreateExpenseAsync("FIN", 50_000m, "Payroll 2026-11");
            Assert.True(accepted.Approved);
            Assert.NotNull(accepted.ExpenseId);
            Assert.Equal(550_000m, accepted.RemainingBudget);

            var refused = await client.CreateExpenseAsync("FIN", 9_000_000m, "Too much");
            Assert.False(refused.Approved);
            Assert.Equal("Insufficient budget", refused.Message);
            Assert.Null(refused.ExpenseId);
        }

        [Fact]
        public async Task AnErrorStatus_BecomesA502WithBudgetsMessage()
        {
            var fake = new FakeBudgetApi { FailWith = HttpStatusCode.ServiceUnavailable };
            var error = await Assert.ThrowsAsync<BudgetServiceException>(() => Client(fake).ListFundingRequestsAsync());

            Assert.Equal(502, error.StatusCode);
            Assert.Contains("503", error.Message);
            Assert.Contains("Budget is down for maintenance", error.Message);
        }

        [Fact]
        public async Task WithoutABaseUrl_EveryCallIsA503()
        {
            var error = await Assert.ThrowsAsync<BudgetNotConfiguredException>(() => Client(new FakeBudgetApi(), null).ListFundingRequestsAsync());
            Assert.Equal(503, error.StatusCode);
        }

        [Theory]
        [InlineData("PTR", "TRANSIT")]
        [InlineData("mnt", "UTIL")]
        [InlineData("HRS", "HR")]
        [InlineData("BUD", "FIN")]
        [InlineData("EMS", "EMS")]
        [InlineData("XYZ", "XYZ")]
        public void DepartmentCodes_MapToBudgetsCodes(string ours, string theirs) =>
            Assert.Equal(theirs, new BudgetOptions().ToBudgetCode(ours));
    }
}
