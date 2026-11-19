/**
 * @file CredentialsApiTests.cs
 * @brief Regression tests for credential endpoints (business rules 5 and 6).
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>
    /// Credentials. Status is derived from the expiry date on every read (test day: 5 Nov 2026).
    /// Assertions on seeded counts filter by EMS or SEC; write tests use new HRS staff.
    /// </summary>
    public class CredentialsApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        private static readonly DateOnly Today = StaffHrApiFactory.Today;

        private async Task<CredentialDto> AddCredentialAsync(string staffNumber, int expiresInDays, int typeId = Seed.CertifiedHrProfessional) =>
            await ReadAsync<CredentialDto>(
                await PostAsync($"/staff/{staffNumber}/credentials",
                    new CreateCredentialRequest(typeId, "FAKE-T-" + Guid.NewGuid().ToString("N")[..6], Today.AddYears(-1), Today.AddDays(expiresInDays))),
                HttpStatusCode.Created);

        // ---- GET /credentials/types ---------------------------------------------------------------------------

        [Fact]
        public async Task Types_ListsTheTwelveSeededTypesByName()
        {
            var types = await GetAsync<List<CredentialTypeDto>>("/credentials/types");

            Assert.Equal(12, types.Count);
            Assert.Equal(types.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).Select(t => t.Name), types.Select(t => t.Name));
            var pcp = types.Single(t => t.CredentialTypeId == Seed.PrimaryCareParamedicLicence);
            Assert.Equal("Primary Care Paramedic Licence", pcp.Name);
            Assert.True(pcp.IsDutyBlocking);
            Assert.False(types.Single(t => t.CredentialTypeId == Seed.CertifiedHrProfessional).IsDutyBlocking);
        }

        [Fact]
        public async Task Types_IdsWorkForCreatingACredential()
        {
            var staff = await CreateStaffAsync();
            var type = (await GetAsync<List<CredentialTypeDto>>("/credentials/types")).First(t => t.Name.StartsWith("CPR"));

            var credential = await ReadAsync<CredentialDto>(
                await PostAsync($"/staff/{staff.StaffNumber}/credentials", new CreateCredentialRequest(type.CredentialTypeId, "FAKE-CPR", Today, Today.AddYears(1))),
                HttpStatusCode.Created);

            Assert.Equal(type.Name, credential.CredentialType);
        }

        // ---- GET /credentials ---------------------------------------------------------------------------------

        [Theory]
        [InlineData("Valid", 10)]
        [InlineData("Expiring", 3)]
        [InlineData("Expired", 2)]
        [InlineData("Suspended", 0)]
        public async Task Search_ByDerivedState_InEms(string state, int expected)
        {
            var result = await GetAsync<PagedResult<CredentialDto>>($"/credentials?state={state}&department=EMS&pageSize=100");

            Assert.Equal(expected, result.Total);
            Assert.All(result.Items, c => Assert.Equal(Enum.Parse<CredentialStatus>(state), c.Status));
            Assert.All(result.Items, c => Assert.Equal("EMS", c.DepartmentCode));
        }

        [Fact]
        public async Task Search_Suspended_FindsNoorsCertificate()
        {
            var result = await GetAsync<PagedResult<CredentialDto>>("/credentials?state=Suspended&department=SEC");

            var suspended = Assert.Single(result.Items);
            Assert.Equal(Seed.Noor, suspended.StaffNumber);
            Assert.True(suspended.DaysUntilExpiry > 30); // suspended even though the date is fine
        }

        [Fact]
        public async Task Search_EveryStatusMatchesItsExpiryDate()
        {
            var result = await GetAsync<PagedResult<CredentialDto>>("/credentials?department=EMS&pageSize=100");

            Assert.Equal(15, result.Total);
            Assert.All(result.Items, c => Assert.Equal(
                CredentialStatusRules.Derive(CredentialStatus.Valid, c.ExpiryDate, Today), c.Status));
            Assert.All(result.Items, c => Assert.Equal(c.ExpiryDate.DayNumber - Today.DayNumber, c.DaysUntilExpiry));
        }

        [Fact]
        public async Task Search_IsPagedAndSortedBySoonestExpiry()
        {
            var page1 = await GetAsync<PagedResult<CredentialDto>>("/credentials?department=EMS&pageSize=5");
            var page2 = await GetAsync<PagedResult<CredentialDto>>("/credentials?department=EMS&pageSize=5&page=2");

            Assert.Equal(5, page1.Items.Count);
            var both = page1.Items.Concat(page2.Items).ToList();
            Assert.Equal(both.OrderBy(c => c.ExpiryDate).Select(c => c.CredentialId), both.Select(c => c.CredentialId));
            Assert.Empty(page1.Items.Select(c => c.CredentialId).Intersect(page2.Items.Select(c => c.CredentialId)));
        }

        [Fact]
        public async Task Search_ByType()
        {
            var result = await GetAsync<PagedResult<CredentialDto>>($"/credentials?type={Seed.PrimaryCareParamedicLicence}&department=EMS");

            Assert.Equal([Seed.Priya, Seed.Jonah, Seed.Ines], result.Items.Select(c => c.StaffNumber).Order());
            Assert.All(result.Items, c => Assert.True(c.IsDutyBlocking));
        }

        [Theory]
        [InlineData("page=0", "page")]
        [InlineData("pageSize=500", "pageSize")]
        [InlineData("department=XYZ", "department")]
        public async Task Search_InvalidFilter_Returns422(string query, string field)
        {
            await AssertProblemAsync(await Client.GetAsync($"/credentials?{query}"), HttpStatusCode.UnprocessableEntity, field);
        }

        [Fact]
        public async Task Search_UnknownState_Returns400()
        {
            await AssertProblemAsync(await Client.GetAsync("/credentials?state=Lapsed"), HttpStatusCode.BadRequest, "state");
        }

        // ---- GET /credentials/expiring ------------------------------------------------------------------------

        [Fact]
        public async Task Expiring_DefaultsTo30DaysSoonestFirst()
        {
            var result = await GetAsync<List<CredentialDto>>("/credentials/expiring");

            Assert.All(result, c => Assert.InRange(c.DaysUntilExpiry, 0, 30));
            Assert.All(result, c => Assert.Equal(CredentialStatus.Expiring, c.Status));
            Assert.Equal(result.OrderBy(c => c.ExpiryDate).Select(c => c.CredentialId), result.Select(c => c.CredentialId));

            // The five seeded expiring credentials, in expiry order (7, 12, 15, 21, 28 days).
            var seeded = result.Where(c => c.Number.StartsWith("FAKE-1")).Select(c => (c.StaffNumber, c.DaysUntilExpiry));
            Assert.Equal([(Seed.Wren, 7), (Seed.Desmond, 12), ("STF-0000020", 15), (Seed.Jonah, 21), ("STF-0000012", 28)], seeded);
        }

        [Fact]
        public async Task Expiring_WithinDaysNarrowsTheWindow()
        {
            var result = await GetAsync<List<CredentialDto>>("/credentials/expiring?withinDays=10");

            Assert.All(result, c => Assert.InRange(c.DaysUntilExpiry, 0, 10));
            Assert.Contains(result, c => c.StaffNumber == Seed.Wren);
            Assert.DoesNotContain(result, c => c.StaffNumber == Seed.Desmond);
        }

        [Fact]
        public async Task Expiring_ExcludesExpiredAndSuspended()
        {
            var result = await GetAsync<List<CredentialDto>>("/credentials/expiring?withinDays=365");

            Assert.DoesNotContain(result, c => c.Status is CredentialStatus.Expired or CredentialStatus.Suspended);
            Assert.DoesNotContain(result, c => c.StaffNumber == Seed.Noor);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(366)]
        public async Task Expiring_WithinDaysOutOfRange_Returns422(int days)
        {
            await AssertProblemAsync(await Client.GetAsync($"/credentials/expiring?withinDays={days}"),
                HttpStatusCode.UnprocessableEntity, "withinDays");
        }

        // ---- POST /staff/{staffNumber}/credentials ---------------------------------------------------------------

        [Theory]
        [InlineData(200, CredentialStatus.Valid)]
        [InlineData(20, CredentialStatus.Expiring)]
        [InlineData(-3, CredentialStatus.Expired)]
        public async Task Create_StatusIsDerivedFromTheExpiryDate(int expiresInDays, CredentialStatus expected)
        {
            var staff = await CreateStaffAsync();

            var credential = await AddCredentialAsync(staff.StaffNumber, expiresInDays);

            Assert.Equal(expected, credential.Status);
            Assert.Equal(expiresInDays, credential.DaysUntilExpiry);
            Assert.Equal("Certified HR Professional", credential.CredentialType);
            Assert.False(credential.IsDutyBlocking);
            Assert.Equal(staff.StaffNumber, credential.StaffNumber);
            Assert.Equal("HRS", credential.DepartmentCode);
        }

        [Fact]
        public async Task Create_ShowsOnTheProfileAndSetsTheWorstState()
        {
            var staff = await CreateStaffAsync();
            await AddCredentialAsync(staff.StaffNumber, 200);
            await AddCredentialAsync(staff.StaffNumber, 20);

            var profile = await GetAsync<StaffProfileDto>($"/staff/{staff.StaffNumber}");

            Assert.Equal(2, profile.Credentials.Count);
            Assert.Equal(CredentialStatus.Expiring, profile.CredentialState);
        }

        [Theory]
        [InlineData(0)]   // same day
        [InlineData(-30)] // before
        public async Task Create_ExpiryNotAfterIssue_Returns422(int expiryOffset)
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(
                await PostAsync($"/staff/{staff.StaffNumber}/credentials",
                    new CreateCredentialRequest(Seed.CertifiedHrProfessional, "FAKE-X", Today, Today.AddDays(expiryOffset))),
                HttpStatusCode.UnprocessableEntity, "expiryDate");
        }

        [Fact]
        public async Task Create_MissingFieldsOrUnknownType_Returns422()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await PostAsync($"/staff/{staff.StaffNumber}/credentials", new { }),
                HttpStatusCode.UnprocessableEntity, "credentialTypeId", "number", "issueDate", "expiryDate");
            await AssertProblemAsync(
                await PostAsync($"/staff/{staff.StaffNumber}/credentials", new CreateCredentialRequest(999, "FAKE-X", Today, Today.AddDays(10))),
                HttpStatusCode.UnprocessableEntity, "credentialTypeId");
        }

        [Fact]
        public async Task Create_UnknownStaff_Returns404()
        {
            await AssertProblemAsync(
                await PostAsync("/staff/STF-9999999/credentials", new CreateCredentialRequest(1, "FAKE-X", Today, Today.AddDays(10))),
                HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_MalformedDate_Returns400()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await PostAsync($"/staff/{staff.StaffNumber}/credentials", "{\"expiryDate\": \"soon\"}"),
                HttpStatusCode.BadRequest, "$.expiryDate");
        }

        // ---- PATCH /credentials/{id} ----------------------------------------------------------------------------

        [Fact]
        public async Task Patch_Renewal_UpdatesNumberDatesAndStatus()
        {
            var staff = await CreateStaffAsync();
            var expiring = await AddCredentialAsync(staff.StaffNumber, 10);

            var renewed = await ReadAsync<CredentialDto>(await PatchAsync($"/credentials/{expiring.CredentialId}",
                new { number = "FAKE-RENEWED", issueDate = Today, expiryDate = Today.AddYears(2) }));

            Assert.Equal("FAKE-RENEWED", renewed.Number);
            Assert.Equal(Today, renewed.IssueDate);
            Assert.Equal(CredentialStatus.Valid, renewed.Status);
        }

        [Fact]
        public async Task Patch_Suspend_OverridesTheDate_AndLiftingRestoresIt()
        {
            var staff = await CreateStaffAsync();
            var credential = await AddCredentialAsync(staff.StaffNumber, 200);

            var suspended = await ReadAsync<CredentialDto>(await PatchAsync($"/credentials/{credential.CredentialId}", new { suspended = true }));
            Assert.Equal(CredentialStatus.Suspended, suspended.Status);

            // Renewing a suspended credential keeps it suspended.
            var renewed = await ReadAsync<CredentialDto>(await PatchAsync($"/credentials/{credential.CredentialId}", new { expiryDate = Today.AddYears(3) }));
            Assert.Equal(CredentialStatus.Suspended, renewed.Status);

            var lifted = await ReadAsync<CredentialDto>(await PatchAsync($"/credentials/{credential.CredentialId}", new { suspended = false }));
            Assert.Equal(CredentialStatus.Valid, lifted.Status);
        }

        [Fact]
        public async Task Patch_ExpiryBeforeIssue_Returns422()
        {
            var staff = await CreateStaffAsync();
            var credential = await AddCredentialAsync(staff.StaffNumber, 200);

            await AssertProblemAsync(await PatchAsync($"/credentials/{credential.CredentialId}", new { expiryDate = Today.AddYears(-2) }),
                HttpStatusCode.UnprocessableEntity, "expiryDate");
        }

        [Fact]
        public async Task Patch_EmptyBodyOrBlankNumber_Returns422()
        {
            var staff = await CreateStaffAsync();
            var credential = await AddCredentialAsync(staff.StaffNumber, 200);

            await AssertProblemAsync(await PatchAsync($"/credentials/{credential.CredentialId}", new { }),
                HttpStatusCode.UnprocessableEntity, "body");
            await AssertProblemAsync(await PatchAsync($"/credentials/{credential.CredentialId}", new { number = " " }),
                HttpStatusCode.UnprocessableEntity, "number");
        }

        [Fact]
        public async Task Patch_UnknownCredential_Returns404()
        {
            await AssertProblemAsync(await PatchAsync("/credentials/999999", new { suspended = true }), HttpStatusCode.NotFound);
        }
    }
}
