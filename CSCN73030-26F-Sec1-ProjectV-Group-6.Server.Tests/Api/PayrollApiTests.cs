/**
 * @file PayrollApiTests.cs
 * @brief Regression tests for compensation history and the payroll headcount for Budget.
 */

using System.Net;
using System.Net.Http.Json;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Pay history (non-overlapping rates) and per-department payroll totals.</summary>
    public class PayrollApiTests : ApiTestBase
    {
        private static readonly DateOnly Today = StaffHrApiFactory.Today;

        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public PayrollApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        private Task<HttpResponseMessage> SetPayAsync(string staffNumber, object body) =>
            Client.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/staff/{staffNumber}/compensation")
            {
                Content = JsonContent.Create(body, options: Json),
            });

        // ---- Compensation -----------------------------------------------------------------------------------

        [Fact]
        public async Task SetPay_FirstRate_IsCurrent()
        {
            var staff = await CreateStaffAsync();

            var history = await ReadAsync<List<CompensationDto>>(
                await SetPayAsync(staff.StaffNumber, new { payBand = "HRS-2", annualSalary = 55_000m, effectiveFrom = Today }));

            var rate = Assert.Single(history);
            Assert.Equal("HRS-2", rate.PayBand);
            Assert.Equal(55_000m, rate.AnnualSalary);
            Assert.Null(rate.EffectiveTo);
        }

        [Fact]
        public async Task SetPay_NewRate_ClosesThePreviousOneOnItsStart()
        {
            var staff = await CreateStaffAsync();
            await SetPayAsync(staff.StaffNumber, new { payBand = "HRS-2", annualSalary = 55_000m, effectiveFrom = Today });

            var history = await ReadAsync<List<CompensationDto>>(
                await SetPayAsync(staff.StaffNumber, new { payBand = "HRS-3", annualSalary = 61_500.50m, effectiveFrom = Today.AddMonths(6) }));

            Assert.Equal(2, history.Count);
            Assert.Equal(("HRS-3", (DateOnly?)null), (history[0].PayBand, history[0].EffectiveTo));   // newest first
            Assert.Equal(Today.AddMonths(6), history[1].EffectiveTo);                               // closed, exclusive
            Assert.Equal(61_500.50m, history[0].AnnualSalary);
        }

        [Fact]
        public async Task SetPay_SameStartDate_CorrectsTheCurrentRate()
        {
            var staff = await CreateStaffAsync();
            await SetPayAsync(staff.StaffNumber, new { payBand = "HRS-2", annualSalary = 55_000m, effectiveFrom = Today });

            var history = await ReadAsync<List<CompensationDto>>(
                await SetPayAsync(staff.StaffNumber, new { payBand = "HRS-2", annualSalary = 56_000m, effectiveFrom = Today }));

            Assert.Equal(56_000m, Assert.Single(history).AnnualSalary);
        }

        [Fact]
        public async Task SetPay_StartingBeforeTheCurrentRate_Returns422()
        {
            var staff = await CreateStaffAsync();
            await SetPayAsync(staff.StaffNumber, new { payBand = "HRS-2", annualSalary = 55_000m, effectiveFrom = Today.AddDays(10) });

            await AssertProblemAsync(await SetPayAsync(staff.StaffNumber, new { payBand = "HRS-2", annualSalary = 50_000m, effectiveFrom = Today.AddDays(5) }),
                HttpStatusCode.UnprocessableEntity, "effectiveFrom");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1_000_001)]
        public async Task SetPay_SalaryOutOfRange_Returns422(int salary)
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await SetPayAsync(staff.StaffNumber, new { payBand = "X", annualSalary = salary, effectiveFrom = Today }),
                HttpStatusCode.UnprocessableEntity, "annualSalary");
        }

        [Fact]
        public async Task SetPay_MissingFieldsOrBeforeHire_Returns422()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await SetPayAsync(staff.StaffNumber, new { }), HttpStatusCode.UnprocessableEntity,
                "payBand", "annualSalary", "effectiveFrom");
            await AssertProblemAsync(await SetPayAsync(staff.StaffNumber, new { payBand = "X", annualSalary = 1000, effectiveFrom = Today.AddDays(-1) }),
                HttpStatusCode.UnprocessableEntity, "effectiveFrom");
        }

        [Fact]
        public async Task SetPay_ForTerminatedEmployee_Returns422()
        {
            await AssertProblemAsync(await SetPayAsync(Seed.Matteo, new { payBand = "PTR-2", annualSalary = 61_000, effectiveFrom = Today }),
                HttpStatusCode.UnprocessableEntity, "staffNumber");
        }

        [Fact]
        public async Task SetPay_UnknownStaff_Returns404()
        {
            await AssertProblemAsync(await SetPayAsync("STF-9999999", new { payBand = "X", annualSalary = 1, effectiveFrom = Today }), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SetPay_IsAudited_ButNeverShownOnTheProfile()
        {
            var staff = await CreateStaffAsync();
            var rate = (await ReadAsync<List<CompensationDto>>(
                await SetPayAsync(staff.StaffNumber, new { payBand = "HRS-2", annualSalary = 55_000m, effectiveFrom = Today })))[0];

            var audit = await GetAsync<PagedResult<AuditLogDto>>($"/audit?entityType=Compensation&entityId={rate.CompensationId}");
            Assert.Equal("Created", Assert.Single(audit.Items).Action);

            var profile = await Client.GetStringAsync($"/staff/{staff.StaffNumber}");
            Assert.DoesNotContain("salary", profile, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("55000", profile);
        }

        [Fact]
        public async Task History_ShowsSeededPromotion()
        {
            var history = await GetAsync<List<CompensationDto>>($"/staff/{Seed.Desmond}/compensation");

            Assert.Equal(["EMS-4", "EMS-3"], history.Select(c => c.PayBand));
            Assert.Null(history[0].EffectiveTo);
            Assert.Equal(history[0].EffectiveFrom, history[1].EffectiveTo);
        }

        [Fact]
        public async Task History_UnknownStaff_Returns404()
        {
            await AssertProblemAsync(await Client.GetAsync("/staff/STF-9999999/compensation"), HttpStatusCode.NotFound);
        }

        // ---- Payroll headcount ----------------------------------------------------------------------------------

        [Fact]
        public async Task Headcount_TotalsEachDepartmentFromSeedData()
        {
            var payroll = await GetAsync<PayrollHeadcountDto>("/headcounts/payroll");
            var rows = payroll.Departments.ToDictionary(d => d.Code);

            Assert.Equal(Today, payroll.AsOf);
            Assert.Equal(7, rows.Count);
            // EMS: 8 employees (Rowan on leave is still paid), Wren part-time; total of the 8 current rates.
            Assert.Equal(new DepartmentPayrollDto("EMS", "Emergency Services", 8, 7, 1, 599_000m, 74_875m, 0), rows["EMS"]);
            // PTR: terminated Matteo is off payroll.
            Assert.Equal(new DepartmentPayrollDto("PTR", "Public Transport", 2, 2, 0, 128_000m, 64_000m, 0), rows["PTR"]);
            // MNT: Bram is seasonal, so counted as part-time.
            Assert.Equal(new DepartmentPayrollDto("MNT", "Maintenance", 2, 1, 1, 118_000m, 59_000m, 0), rows["MNT"]);
            Assert.Equal(241_000m, rows["SEC"].TotalAnnualSalary);
            Assert.Equal(payroll.Departments.Sum(d => d.Headcount), payroll.TotalHeadcount);
            Assert.Equal(payroll.Departments.Sum(d => d.TotalAnnualSalary), payroll.TotalAnnualSalary);
        }

        [Fact]
        public async Task Headcount_AsOfAnEarlierDate_UsesTheRatesInForceThen()
        {
            // Four months ago Ines (hired three months ago) was not on payroll yet.
            var payroll = await GetAsync<PayrollHeadcountDto>($"/headcounts/payroll?asOf={Today.AddMonths(-4):yyyy-MM-dd}");
            var ems = payroll.Departments.Single(d => d.Code == "EMS");

            Assert.Equal(7, ems.Headcount);
            Assert.Equal(599_000m - 68_000m, ems.TotalAnnualSalary);
        }

        [Fact]
        public async Task Headcount_CountsEmployeesWithoutARate()
        {
            var before = (await GetAsync<PayrollHeadcountDto>("/headcounts/payroll")).Departments.Single(d => d.Code == "HRS");
            await CreateStaffAsync();

            var after = (await GetAsync<PayrollHeadcountDto>("/headcounts/payroll")).Departments.Single(d => d.Code == "HRS");
            Assert.Equal(before.Headcount + 1, after.Headcount);
            Assert.Equal(before.MissingCompensation + 1, after.MissingCompensation);
            Assert.Equal(before.TotalAnnualSalary, after.TotalAnnualSalary);
        }

        [Fact]
        public async Task Headcount_UnreadableDate_Returns400()
        {
            await AssertProblemAsync(await Client.GetAsync("/headcounts/payroll?asOf=today"), HttpStatusCode.BadRequest, "asOf");
        }
    }
}
