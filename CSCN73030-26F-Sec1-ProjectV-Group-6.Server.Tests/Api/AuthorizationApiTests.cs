/**
 * @file AuthorizationApiTests.cs
 * @brief Regression tests for role-based access control (via the development sign-in headers).
 */

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Each policy lets the right roles through and answers 401 / 403 as ProblemDetails otherwise.</summary>
    public class AuthorizationApiTests : ApiTestBase
    {
        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public AuthorizationApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        private Task<HttpResponseMessage> SendAsAsync(HttpMethod method, string url, string? roles, object? body = null,
            string? idempotencyKey = null, bool anonymous = false, string? user = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (body != null) request.Content = JsonContent.Create(body, options: Json);
            if (roles != null) request.Headers.Add("X-Dev-Roles", roles);
            if (anonymous) request.Headers.Add("X-Dev-Anonymous", "true");
            if (user != null) request.Headers.Add("X-Dev-User", user);
            if (idempotencyKey != null) request.Headers.Add("Idempotency-Key", idempotencyKey);
            return Client.SendAsync(request);
        }

        // ---- 401 ------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData("/staff")]
        [InlineData("/departments")]
        [InlineData("/requisitions")]
        [InlineData("/headcounts/payroll")]
        [InlineData("/audit")]
        public async Task NotSignedIn_Returns401Problem(string url)
        {
            var problem = await AssertProblemAsync(await SendAsAsync(HttpMethod.Get, url, null, anonymous: true), HttpStatusCode.Unauthorized);
            Assert.Contains("token", problem.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Health_IsOpenToEveryone()
        {
            Assert.Equal(HttpStatusCode.OK, (await SendAsAsync(HttpMethod.Get, "/health", null, anonymous: true)).StatusCode);
        }

        [Fact]
        public async Task SignedInWithoutAnyRole_Returns403()
        {
            await AssertProblemAsync(await SendAsAsync(HttpMethod.Get, "/staff", "none"), HttpStatusCode.Forbidden);
        }

        // ---- Role matrix ----------------------------------------------------------------------------------------

        /// <summary>(method, url, role, allowed). Rows marked allowed may still fail validation; only 401/403 matter here.</summary>
        public static readonly TheoryData<string, string, string, bool> Matrix = new()
        {
            // Reading: any role.
            { "GET", "/staff", StaffHrRoles.Read, true },
            { "GET", "/staff", StaffHrRoles.Payroll, true },
            { "GET", "/rosters/on-duty", StaffHrRoles.RostersWrite, true },
            { "GET", $"/staff/{Seed.Talia}/eligibility", StaffHrRoles.Read, true },
            // HR changes: Admin only.
            { "POST", "/staff", StaffHrRoles.Read, false },
            { "POST", "/staff", StaffHrRoles.RequisitionsSubmit, false },
            { "PATCH", $"/staff/{Seed.Talia}", StaffHrRoles.Read, false },
            { "POST", $"/staff/{Seed.Talia}/credentials", StaffHrRoles.Read, false },
            { "PATCH", "/requisitions/REQ-0000001", StaffHrRoles.RequisitionsSubmit, false },
            // Rosters: Admin or Rosters.Write.
            { "POST", "/rosters/entries", StaffHrRoles.Read, false },
            { "POST", "/rosters/entries", StaffHrRoles.RostersWrite, true },
            { "DELETE", "/rosters/entries/999999", StaffHrRoles.RostersWrite, true },
            // Requisitions: Admin or Requisitions.Submit.
            { "POST", "/requisitions", StaffHrRoles.Read, false },
            { "POST", "/requisitions", StaffHrRoles.RequisitionsSubmit, true },
            // Security flags: Admin or SecurityFlags.Write, even to read.
            { "GET", "/security-flags", StaffHrRoles.Read, false },
            { "PUT", $"/staff/{Seed.Talia}/security-flags/X", StaffHrRoles.Read, false },
            { "GET", "/security-flags", StaffHrRoles.SecurityFlagsWrite, true },
            // Salary: Payroll only, not even Admin.
            { "GET", "/headcounts/payroll", StaffHrRoles.Admin, false },
            { "GET", $"/staff/{Seed.Talia}/compensation", StaffHrRoles.Admin, false },
            { "GET", "/headcounts/payroll", StaffHrRoles.Payroll, true },
            // Audit: Admin or Audit.Read.
            { "GET", "/audit", StaffHrRoles.Read, false },
            { "GET", "/audit", StaffHrRoles.AuditRead, true },
            { "GET", "/audit", StaffHrRoles.Admin, true },
        };

        [Theory]
        [MemberData(nameof(Matrix))]
        public async Task Roles_AreEnforced(string method, string url, string role, bool allowed)
        {
            var body = method is "GET" or "DELETE" ? null : new { };
            var response = await SendAsAsync(new HttpMethod(method), url, role, body, idempotencyKey: Guid.NewGuid().ToString());

            if (allowed)
                Assert.True(response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden),
                    $"{method} {url} as {role} returned {(int)response.StatusCode}");
            else
                await AssertProblemAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task RosterScheduler_CanAddAShift()
        {
            var staff = await CreateStaffAsync();
            var serviceId = await ServiceIdAsync("HRS", "Recruitment");

            var response = await SendAsAsync(HttpMethod.Post, "/rosters/entries", StaffHrRoles.RostersWrite, new
            {
                staffNumber = staff.StaffNumber, serviceId, shiftType = "Day",
                shiftStart = "2026-12-21T08:00:00Z", shiftEnd = "2026-12-21T16:00:00Z",
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // ---- Who did it ----------------------------------------------------------------------------------------

        [Fact]
        public async Task Audit_RecordsTheSignedInUser()
        {
            var request = NewStaffRequest();
            var created = await ReadAsync<StaffProfileDto>(
                await SendAsAsync(HttpMethod.Post, "/staff", StaffHrRoles.Admin, request, user: "alice.admin@cmes.example"),
                HttpStatusCode.Created);

            var audit = await GetAsync<PagedResult<AuditLogDto>>($"/audit?entityType=Staff&entityId={created.StaffNumber}");
            Assert.Equal("alice.admin@cmes.example", Assert.Single(audit.Items).ActorId);
        }

        // ---- Contract ------------------------------------------------------------------------------------------

        [Fact]
        public async Task OpenApi_DocumentsBearerAuthAnd401403()
        {
            using var doc = JsonDocument.Parse(await Client.GetStringAsync("/openapi/v1.json"));
            var root = doc.RootElement;

            var scheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
            Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());

            var staffGet = root.GetProperty("paths").GetProperty("/staff").GetProperty("get");
            Assert.True(staffGet.GetProperty("responses").TryGetProperty("401", out _));
            Assert.True(staffGet.GetProperty("responses").TryGetProperty("403", out _));
            Assert.True(staffGet.TryGetProperty("security", out _));

            var health = root.GetProperty("paths").GetProperty("/health").GetProperty("get");
            Assert.False(health.GetProperty("responses").TryGetProperty("401", out _));
        }
    }
}
