/**
 * @file EligibilityApiTests.cs
 * @brief Regression tests for GET /staff/{staffNumber}/eligibility.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Eligibility of seeded people on the test day (5 Nov 2026), whose situations cover every rule.</summary>
    public class EligibilityApiTests : ApiTestBase
    {
        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public EligibilityApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        private Task<DutyEligibilityDto> CheckAsync(string staffNumber, string query = "") =>
            GetAsync<DutyEligibilityDto>($"/staff/{staffNumber}/eligibility{query}");

        [Fact]
        public async Task ValidParamedic_IsEligibleWithoutWarnings()
        {
            var result = await CheckAsync(Seed.Talia);

            Assert.True(result.Eligible);
            Assert.Empty(result.Reasons);
            Assert.Empty(result.Warnings);
            Assert.Equal(StaffHrApiFactory.Now.UtcDateTime, result.At);
            Assert.Null(result.ServiceId);
        }

        [Fact]
        public async Task ExpiringCpr_IsEligibleWithAWarning()
        {
            var result = await CheckAsync(Seed.Desmond);

            Assert.True(result.Eligible);
            var warning = Assert.Single(result.Warnings);
            Assert.Equal("CREDENTIAL_EXPIRING", warning.Code);
            Assert.Contains("2026-11-17", warning.Message);
        }

        [Theory]
        [InlineData(Seed.Priya, "CREDENTIAL_EXPIRED")]  // CPR expired 5 days ago
        [InlineData(Seed.Kofi, "CREDENTIAL_EXPIRED")]   // ACP licence expired
        [InlineData(Seed.Rowan, "EMPLOYMENT_STATUS")]   // on leave
        public async Task SingleProblem_IsIneligibleForThatReason(string staffNumber, string code)
        {
            var result = await CheckAsync(staffNumber);

            Assert.False(result.Eligible);
            Assert.Equal(code, Assert.Single(result.Reasons).Code);
        }

        [Fact]
        public async Task SuspendedFlaggedOfficer_ListsEveryReason()
        {
            var result = await CheckAsync(Seed.Noor);

            Assert.False(result.Eligible);
            Assert.Equal(["EMPLOYMENT_STATUS", "CREDENTIAL_SUSPENDED", "SECURITY_FLAG_ACTIVE"], result.Reasons.Select(r => r.Code));
        }

        [Fact]
        public async Task TerminatedEmployee_HasNoCurrentAssignment()
        {
            var codes = (await CheckAsync(Seed.Matteo)).Reasons.Select(r => r.Code).ToList();

            Assert.Contains("EMPLOYMENT_STATUS", codes);
            Assert.Contains("NO_CURRENT_ASSIGNMENT", codes);
        }

        [Fact]
        public async Task ForAService_RequiresAnAssignmentInThatService()
        {
            var fireService = await ServiceIdAsync("EMS", "Fire & Rescue");

            Assert.True((await CheckAsync(Seed.Talia, $"?service={Seed.ParamedicServiceId}")).Eligible);

            var fire = await CheckAsync(Seed.Talia, $"?service={fireService}");
            Assert.False(fire.Eligible);
            Assert.Equal("NO_CURRENT_ASSIGNMENT", Assert.Single(fire.Reasons).Code);
            Assert.Equal(fireService, fire.ServiceId);
        }

        [Fact]
        public async Task AtAFutureDate_UsesThatDatesCredentialStatus()
        {
            // Desmond's CPR certificate (duty-blocking) expires on 17 Nov.
            var later = await CheckAsync(Seed.Desmond, "?at=2026-11-20T08:00:00Z");

            Assert.False(later.Eligible);
            Assert.Equal("CREDENTIAL_EXPIRED", Assert.Single(later.Reasons).Code);
        }

        [Fact]
        public async Task ActiveFlag_BlocksDuty_UntilCleared()
        {
            var serviceId = await ServiceIdAsync("HRS", "Recruitment");
            var staff = await CreateStaffAsync(new CreateAssignmentRequest(serviceId, "Recruiter", null, null));
            Assert.True((await CheckAsync(staff.StaffNumber)).Eligible);

            await SendPutAsync($"/staff/{staff.StaffNumber}/security-flags/UnderInvestigation", new { status = "Active", updatedAt = "2026-11-05T10:00:00Z" });
            Assert.False((await CheckAsync(staff.StaffNumber)).Eligible);

            await SendPutAsync($"/staff/{staff.StaffNumber}/security-flags/UnderInvestigation", new { status = "Cleared", updatedAt = "2026-11-05T11:00:00Z" });
            Assert.True((await CheckAsync(staff.StaffNumber)).Eligible);
        }

        [Fact]
        public async Task NewHireWithoutAssignment_IsIneligible()
        {
            var staff = await CreateStaffAsync();
            Assert.Equal("NO_CURRENT_ASSIGNMENT", Assert.Single((await CheckAsync(staff.StaffNumber)).Reasons).Code);
        }

        [Fact]
        public async Task UnknownStaff_Returns404()
        {
            await AssertProblemAsync(await Client.GetAsync("/staff/STF-9999999/eligibility"), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UnknownService_Returns422()
        {
            await AssertProblemAsync(await Client.GetAsync($"/staff/{Seed.Talia}/eligibility?service=9999"), HttpStatusCode.UnprocessableEntity, "service");
        }

        [Fact]
        public async Task UnreadableInstant_Returns400()
        {
            await AssertProblemAsync(await Client.GetAsync($"/staff/{Seed.Talia}/eligibility?at=tomorrow"), HttpStatusCode.BadRequest, "at");
        }

        private async Task SendPutAsync(string url, object body)
        {
            var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = System.Net.Http.Json.JsonContent.Create(body, options: Json),
            });
            response.EnsureSuccessStatusCode();
        }
    }
}
