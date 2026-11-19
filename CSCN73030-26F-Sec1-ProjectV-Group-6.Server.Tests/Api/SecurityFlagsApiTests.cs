/**
 * @file SecurityFlagsApiTests.cs
 * @brief Regression tests for security flag sync with the Security service.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>PUT upserts a flag per staff member and type; stale updates are ignored.</summary>
    public class SecurityFlagsApiTests : ApiTestBase
    {
        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public SecurityFlagsApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        private Task<HttpResponseMessage> PutFlagAsync(string staffNumber, string flagType, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"/staff/{staffNumber}/security-flags/{flagType}")
            {
                Content = System.Net.Http.Json.JsonContent.Create(body, options: Json),
            };
            return Client.SendAsync(request);
        }

        [Fact]
        public async Task Put_NewFlag_Returns201()
        {
            var staff = await CreateStaffAsync();

            var response = await PutFlagAsync(staff.StaffNumber, "UnderInvestigation", new { status = "Active" });
            var flag = await ReadAsync<SecurityFlagDto>(response, HttpStatusCode.Created);

            Assert.Equal("created", response.Headers.GetValues("Sync-Result").Single());
            Assert.Equal(SecurityFlagStatus.Active, flag.Status);
            Assert.Equal(staff.StaffNumber, flag.StaffNumber);
            Assert.Equal(StaffHrApiFactory.Now.UtcDateTime, flag.UpdatedAt); // defaults to now
        }

        [Fact]
        public async Task Put_ExistingFlag_UpdatesInPlace()
        {
            var staff = await CreateStaffAsync();
            var created = await ReadAsync<SecurityFlagDto>(
                await PutFlagAsync(staff.StaffNumber, "WeaponsRestriction", new { status = "Active", updatedAt = "2026-11-01T09:00:00Z" }),
                HttpStatusCode.Created);

            var response = await PutFlagAsync(staff.StaffNumber, "WeaponsRestriction", new { status = "Cleared", updatedAt = "2026-11-03T09:00:00Z" });
            var updated = await ReadAsync<SecurityFlagDto>(response);

            Assert.Equal("updated", response.Headers.GetValues("Sync-Result").Single());
            Assert.Equal(created.FlagId, updated.FlagId);
            Assert.Equal(SecurityFlagStatus.Cleared, updated.Status);
            Assert.Single(await GetAsync<List<SecurityFlagDto>>($"/staff/{staff.StaffNumber}/security-flags"));
        }

        [Fact]
        public async Task Put_OlderUpdate_IsIgnored()
        {
            var staff = await CreateStaffAsync();
            await PutFlagAsync(staff.StaffNumber, "UnderInvestigation", new { status = "Cleared", updatedAt = "2026-11-04T09:00:00Z" });

            // A delayed message from earlier arrives late.
            var response = await PutFlagAsync(staff.StaffNumber, "UnderInvestigation", new { status = "Active", updatedAt = "2026-11-02T09:00:00Z" });
            var flag = await ReadAsync<SecurityFlagDto>(response);

            Assert.Equal("ignored-stale", response.Headers.GetValues("Sync-Result").Single());
            Assert.Equal(SecurityFlagStatus.Cleared, flag.Status);
        }

        [Fact]
        public async Task Put_SameUpdateTwice_IsHarmless()
        {
            var staff = await CreateStaffAsync();
            var body = new { status = "Active", updatedAt = "2026-11-04T09:00:00Z" };
            await PutFlagAsync(staff.StaffNumber, "Retry", body);

            var again = await PutFlagAsync(staff.StaffNumber, "Retry", body);

            Assert.Equal(HttpStatusCode.OK, again.StatusCode);
            Assert.Single(await GetAsync<List<SecurityFlagDto>>($"/staff/{staff.StaffNumber}/security-flags"));
        }

        [Fact]
        public async Task Put_IsAudited()
        {
            var staff = await CreateStaffAsync();
            var flag = await ReadAsync<SecurityFlagDto>(
                await PutFlagAsync(staff.StaffNumber, "Audited", new { status = "Active" }), HttpStatusCode.Created);

            var audit = await GetAsync<PagedResult<AuditLogDto>>($"/audit?entityType=SecurityFlag&entityId={flag.FlagId}");
            Assert.Equal("Created", Assert.Single(audit.Items).Action);
        }

        [Theory]
        [InlineData("has space")]
        [InlineData("semi;colon")]
        public async Task Put_InvalidFlagType_Returns422(string flagType)
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await PutFlagAsync(staff.StaffNumber, Uri.EscapeDataString(flagType), new { status = "Active" }),
                HttpStatusCode.UnprocessableEntity, "flagType");
        }

        [Fact]
        public async Task Put_FlagTypeOver50Characters_Returns422()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await PutFlagAsync(staff.StaffNumber, new string('x', 51), new { status = "Active" }),
                HttpStatusCode.UnprocessableEntity, "flagType");
        }

        [Fact]
        public async Task Put_MissingStatus_Returns422()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await PutFlagAsync(staff.StaffNumber, "X", new { }), HttpStatusCode.UnprocessableEntity, "status");
        }

        [Fact]
        public async Task Put_UnknownStatus_Returns400()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await PutFlagAsync(staff.StaffNumber, "X", new { status = "Open" }), HttpStatusCode.BadRequest, "$.status");
        }

        [Fact]
        public async Task Put_UnknownStaff_Returns404()
        {
            await AssertProblemAsync(await PutFlagAsync("STF-9999999", "X", new { status = "Active" }), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task List_ReturnsSeededFlags()
        {
            var noor = Assert.Single(await GetAsync<List<SecurityFlagDto>>($"/staff/{Seed.Noor}/security-flags"));
            Assert.Equal("UnderInvestigation", noor.FlagType);
            Assert.Equal(SecurityFlagStatus.Active, noor.Status);

            var anders = Assert.Single(await GetAsync<List<SecurityFlagDto>>($"/staff/{Seed.Anders}/security-flags"));
            Assert.Equal(SecurityFlagStatus.Cleared, anders.Status);
        }

        [Fact]
        public async Task List_UnknownStaff_Returns404()
        {
            await AssertProblemAsync(await Client.GetAsync("/staff/STF-9999999/security-flags"), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Search_FiltersByStatusAndType()
        {
            var active = await GetAsync<List<SecurityFlagDto>>("/security-flags?status=Active&flagType=UnderInvestigation");

            Assert.Contains(active, f => f.StaffNumber == Seed.Noor);
            Assert.All(active, f => Assert.Equal(SecurityFlagStatus.Active, f.Status));
            Assert.All(active, f => Assert.Equal("UnderInvestigation", f.FlagType));
            Assert.DoesNotContain(active, f => f.StaffNumber == Seed.Anders);
        }
    }
}
