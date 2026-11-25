/**
 * @file BudgetLedgerApiTests.cs
 * @brief Regression tests for department budgets read from Budget and payroll statements sent to it.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>GET /departments/{code}/budget and /headcounts/payroll/statements, against the fake Finance API.</summary>
    public class BudgetLedgerApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        private int ExpenseCalls() => Factory.Budget.Calls.Count(c => c.Call == "POST /api/budget/v1/expenses");

        private Task<HttpResponseMessage> SendStatementsAsync(string period) =>
            PostAsync("/headcounts/payroll/statements", new { period });

        // ---- Department budget ------------------------------------------------------------------------

        [Fact]
        public async Task DepartmentBudget_ReadsBudgetsFiguresUnderBudgetsCode()
        {
            var budget = await GetAsync<DepartmentBudgetDto>("/departments/PTR/budget");

            Assert.Equal("PTR", budget.Code);
            Assert.Equal("Public Transport", budget.Name);
            Assert.Equal("TRANSIT", budget.BudgetDepartmentCode);
            Assert.Equal(4_000_000m, budget.Allocated);
            Assert.Equal(budget.Allocated - budget.Spent, budget.Remaining);
            Assert.Equal("GET /api/budget/v1/budgets/TRANSIT", Factory.Budget.Calls.Last().Call);
        }

        [Fact]
        public async Task DepartmentBudget_UnknownDepartmentIs404()
        {
            await AssertProblemAsync(await Client.GetAsync("/departments/XYZ/budget"), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DepartmentBudget_WhenBudgetIsDown_Is502()
        {
            Factory.Budget.FailWith = HttpStatusCode.InternalServerError;
            try
            {
                await AssertProblemAsync(await Client.GetAsync("/departments/EMS/budget"), HttpStatusCode.BadGateway);
            }
            finally
            {
                Factory.Budget.FailWith = null;
            }
        }

        // ---- Payroll statements -----------------------------------------------------------------------

        [Fact]
        public async Task SendStatements_RecordsEachDepartmentsMonthlyCostOnce()
        {
            var payroll = await GetAsync<PayrollHeadcountDto>("/headcounts/payroll?asOf=2026-09-30");
            var before = ExpenseCalls();

            var sent = await ReadAsync<PayrollStatementsDto>(await SendStatementsAsync("2026-09"));

            Assert.Equal("2026-09", sent.Period);
            Assert.Equal(new DateOnly(2026, 9, 30), sent.AsOf);
            var expected = payroll.Departments.Where(d => d.TotalAnnualSalary > 0).ToList();
            Assert.Equal(expected.Count, sent.Statements.Count);
            Assert.Equal(expected.Count, ExpenseCalls() - before);
            foreach (var department in expected)
            {
                var statement = sent.Statements.Single(s => s.DepartmentCode == department.Code);
                Assert.Equal(Math.Round(department.TotalAnnualSalary / 12m, 2), statement.Amount);
                Assert.Equal(department.Headcount, statement.Headcount);
                Assert.True(statement.Accepted);
                Assert.NotNull(statement.ExpenseId);
            }
            Assert.Equal("HR", sent.Statements.Single(s => s.DepartmentCode == "HRS").BudgetDepartmentCode);
            var hrsCall = Factory.Budget.Calls.Last(c => c.Call == "POST /api/budget/v1/expenses" && c.Body!["departmentCode"]!.GetValue<string>() == "HR");
            Assert.StartsWith("Staff/HR payroll 2026-09", hrsCall.Body!["description"]!.GetValue<string>());

            // Sending the same month again records nothing twice.
            var again = await ReadAsync<PayrollStatementsDto>(await SendStatementsAsync("2026-09"));
            Assert.Equal(expected.Count, ExpenseCalls() - before);
            Assert.Equal(sent.TotalAmount, again.TotalAmount);

            var listed = await GetAsync<PayrollStatementsDto>("/headcounts/payroll/statements?period=2026-09");
            Assert.Equal(sent.Statements.Select(s => s.DepartmentCode), listed.Statements.Select(s => s.DepartmentCode));
        }

        [Fact]
        public async Task SendStatements_ARefusedDepartmentIsSentAgainNextTime()
        {
            Factory.Budget.SetSpent("FIN", 599_999m);
            var first = await ReadAsync<PayrollStatementsDto>(await SendStatementsAsync("2026-08"));
            var refused = first.Statements.Single(s => s.DepartmentCode == "BUD");
            Assert.False(refused.Accepted);
            Assert.Equal("Insufficient budget", refused.Message);
            Assert.Null(refused.ExpenseId);

            Factory.Budget.SetSpent("FIN", 0m);
            var before = ExpenseCalls();
            var second = await ReadAsync<PayrollStatementsDto>(await SendStatementsAsync("2026-08"));

            Assert.Equal(1, ExpenseCalls() - before);
            Assert.True(second.Statements.Single(s => s.DepartmentCode == "BUD").Accepted);
        }

        [Theory]
        [InlineData("2026-12")]
        [InlineData("2026-13")]
        [InlineData("November")]
        public async Task SendStatements_AFutureOrBadMonthIs422(string period)
        {
            await AssertProblemAsync(await SendStatementsAsync(period), HttpStatusCode.UnprocessableEntity, "period");
        }

        [Fact]
        public async Task ListStatements_AMonthWithNoneIsEmpty()
        {
            var listed = await GetAsync<PayrollStatementsDto>("/headcounts/payroll/statements?period=2025-01");
            Assert.Empty(listed.Statements);
            Assert.Equal(0m, listed.TotalAmount);
        }

        [Fact]
        public async Task SendStatements_NeedsThePayrollRole()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/headcounts/payroll/statements")
            {
                Content = System.Net.Http.Json.JsonContent.Create(new { period = "2026-07" }),
            };
            request.Headers.Add("X-Dev-Roles", StaffHrRoles.Admin);

            await AssertProblemAsync(await Client.SendAsync(request), HttpStatusCode.Forbidden);
        }
    }
}
