/**
 * @file StaffSearchApiTests.cs
 * @brief Regression tests for GET /staff (the staff directory search).
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Directory search: filters, paging and validation. Asserts on EMS/SEC/PTR, which no test writes to.</summary>
    public class StaffSearchApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        private Task<PagedResult<StaffSummaryDto>> SearchAsync(string query) =>
            GetAsync<PagedResult<StaffSummaryDto>>($"/staff?{query}");

        [Fact]
        public async Task Search_ReturnsAPagedEnvelopeSortedByLastName()
        {
            var page1 = await SearchAsync("department=EMS&pageSize=3");
            var page3 = await SearchAsync("department=EMS&pageSize=3&page=3");

            Assert.Equal(8, page1.Total);
            Assert.Equal(1, page1.Page);
            Assert.Equal(3, page1.PageSize);
            Assert.Equal(["Achterberg", "Brennwood", "Castellanos"], page1.Items.Select(s => s.LastName));
            Assert.Equal(["Tibbets", "Valcourt"], page3.Items.Select(s => s.LastName));
        }

        [Fact]
        public async Task Search_DefaultsToPage1Of20()
        {
            var result = await SearchAsync("");
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
            Assert.True(result.Total >= 20);
        }

        [Theory]
        [InlineData("q=STF-0000003", Seed.Priya)]       // staff number
        [InlineData("q=valcourt", Seed.Priya)]          // last name, any case
        [InlineData("q=Des%20Ach", Seed.Desmond)]       // preferred name + last name
        [InlineData("q=Desmond%20Achterberg", Seed.Desmond)] // full name
        [InlineData("q=wren.castellanos", Seed.Wren)]   // email
        public async Task Search_TextMatchesNumberNamesAndEmail(string query, string expected)
        {
            var result = await SearchAsync(query);
            Assert.Equal(expected, Assert.Single(result.Items).StaffNumber);
        }

        [Fact]
        public async Task Search_ByStatus()
        {
            var result = await SearchAsync("status=OnLeave&department=EMS");
            Assert.Equal(Seed.Rowan, Assert.Single(result.Items).StaffNumber);
        }

        [Fact]
        public async Task Search_ByServiceMatchesCurrentAssignmentsOnly()
        {
            var result = await SearchAsync($"service={Seed.ParamedicServiceId}&department=EMS&pageSize=100");

            Assert.Equal([Seed.Talia, Seed.Desmond, Seed.Priya, Seed.Jonah, Seed.Ines, Seed.Kofi],
                result.Items.Select(s => s.StaffNumber).Order());
        }

        [Fact]
        public async Task Search_ByRoleMatchesCurrentRoleTitles()
        {
            var result = await SearchAsync("role=Advanced%20Care&department=EMS");
            Assert.Equal([Seed.Desmond, Seed.Kofi], result.Items.Select(s => s.StaffNumber).Order());
        }

        [Fact]
        public async Task Search_ByRole_IgnoresEndedAssignments()
        {
            // Matteo's only assignment (Fleet Mechanic) ended when he was terminated.
            var result = await SearchAsync("role=Fleet%20Mechanic");
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task Search_ByCredentialState()
        {
            var expired = await SearchAsync("credentialState=Expired&department=EMS");
            var suspended = await SearchAsync("credentialState=Suspended&department=SEC");

            Assert.Equal([Seed.Priya, Seed.Kofi], expired.Items.Select(s => s.StaffNumber).Order());
            Assert.Equal(Seed.Noor, Assert.Single(suspended.Items).StaffNumber);
        }

        [Fact]
        public async Task Search_FiltersCombine()
        {
            var result = await SearchAsync($"department=EMS&service={Seed.ParamedicServiceId}&status=Probation");
            Assert.Equal(Seed.Ines, Assert.Single(result.Items).StaffNumber);
        }

        [Fact]
        public async Task Search_SummaryRowShowsCurrentRoleAndWorstCredential()
        {
            var priya = Assert.Single((await SearchAsync($"q={Seed.Priya}")).Items);

            Assert.Equal("Primary Care Paramedic", priya.CurrentRole);
            Assert.Equal("Paramedic Service", priya.CurrentService);
            Assert.Equal(CredentialStatus.Expired, priya.CredentialState); // one expired CPR outranks two valid
            Assert.Equal(EmploymentType.FullTime, priya.EmploymentType);
        }

        [Fact]
        public async Task Search_NeverReturnsSalary()
        {
            var json = await Client.GetStringAsync("/staff?pageSize=100");
            Assert.DoesNotContain("salary", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("payBand", json, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("page=0", "page")]
        [InlineData("pageSize=0", "pageSize")]
        [InlineData("pageSize=101", "pageSize")]
        [InlineData("department=XYZ", "department")]
        public async Task Search_InvalidFilter_Returns422(string query, string field)
        {
            await AssertProblemAsync(await Client.GetAsync($"/staff?{query}"), HttpStatusCode.UnprocessableEntity, field);
        }

        [Theory]
        [InlineData("status=Retired", "status")]
        [InlineData("pageSize=abc", "pageSize")]
        [InlineData("credentialState=Lapsed", "credentialState")]
        public async Task Search_UnreadableQueryValue_Returns400WithCamelCaseField(string query, string field)
        {
            await AssertProblemAsync(await Client.GetAsync($"/staff?{query}"), HttpStatusCode.BadRequest, field);
        }
    }
}
