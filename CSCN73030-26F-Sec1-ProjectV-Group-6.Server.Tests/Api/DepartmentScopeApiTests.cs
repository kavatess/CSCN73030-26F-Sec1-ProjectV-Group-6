/**
 * @file DepartmentScopeApiTests.cs
 * @brief Regression tests for the department-scoped EF Core query filters (row-level security).
 */

using System.Net;
using System.Net.Http.Json;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>A caller with a department claim sees only its own department's people and records.</summary>
    public class DepartmentScopeApiTests : ApiTestBase
    {
        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public DepartmentScopeApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        private Task<HttpResponseMessage> AsEmsAsync(HttpMethod method, string url, string roles = StaffHrRoles.Read,
            object? body = null, string department = "EMS")
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Dev-Roles", roles);
            request.Headers.Add("X-Dev-Department", department);
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            if (body != null) request.Content = JsonContent.Create(body, options: Json);
            return Client.SendAsync(request);
        }

        private async Task<T> ReadAsEmsAsync<T>(string url) => await ReadAsync<T>(await AsEmsAsync(HttpMethod.Get, url));

        [Fact]
        public async Task Search_ShowsOnlyTheCallersDepartment()
        {
            var result = await ReadAsEmsAsync<PagedResult<StaffSummaryDto>>("/staff?pageSize=100");

            Assert.Equal(8, result.Total);
            Assert.All(result.Items, s => Assert.Equal("EMS", s.HomeDepartment.Code));
        }

        [Fact]
        public async Task OtherDepartmentsStaff_AreNotFound()
        {
            Assert.Equal(HttpStatusCode.OK, (await AsEmsAsync(HttpMethod.Get, $"/staff/{Seed.Talia}")).StatusCode);
            await AssertProblemAsync(await AsEmsAsync(HttpMethod.Get, $"/staff/{Seed.Noor}"), HttpStatusCode.NotFound);
            await AssertProblemAsync(await AsEmsAsync(HttpMethod.Get, $"/staff/{Seed.Noor}/eligibility"), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Credentials_AreFilteredToo()
        {
            var all = await ReadAsEmsAsync<PagedResult<CredentialDto>>("/credentials?pageSize=100");

            Assert.Equal(15, all.Total);
            Assert.All(all.Items, c => Assert.Equal("EMS", c.DepartmentCode));
            Assert.All(await ReadAsEmsAsync<List<CredentialDto>>("/credentials/expiring"), c => Assert.Equal("EMS", c.DepartmentCode));
        }

        [Fact]
        public async Task Requisitions_ShowOnlyThoseRaisedByTheDepartment()
        {
            var requisitions = await ReadAsEmsAsync<List<RequisitionDto>>("/requisitions");

            Assert.NotEmpty(requisitions);
            Assert.All(requisitions, r => Assert.Equal("EMS", r.Department.Code));
            await AssertProblemAsync(await AsEmsAsync(HttpMethod.Get, "/requisitions/REQ-0000003"), HttpStatusCode.NotFound); // SEC's
        }

        [Fact]
        public async Task Headcount_CountsOnlyTheCallersDepartment()
        {
            var rows = (await ReadAsEmsAsync<List<DepartmentHeadcountDto>>("/departments/headcount")).ToDictionary(r => r.Code);

            Assert.Equal(7, rows["EMS"].Active);
            Assert.Equal(0, rows["SEC"].Active);
        }

        [Fact]
        public async Task RosterWriter_CannotRosterAnotherDepartmentsStaff()
        {
            var patrol = await ServiceIdAsync("SEC", "Patrol Division");

            await AssertProblemAsync(await AsEmsAsync(HttpMethod.Post, "/rosters/entries", StaffHrRoles.RostersWrite, new
            {
                staffNumber = "STF-0000009", serviceId = patrol, shiftType = "Day",
                shiftStart = "2026-12-22T08:00:00Z", shiftEnd = "2026-12-22T16:00:00Z",
            }), HttpStatusCode.UnprocessableEntity, "staffNumber");
        }

        [Fact]
        public async Task Submitter_CanOnlyRaiseRequisitionsForItsDepartment()
        {
            var own = await AsEmsAsync(HttpMethod.Post, "/requisitions", StaffHrRoles.RequisitionsSubmit,
                new { departmentCode = "EMS", roleTitle = "Paramedic", positions = 1 });
            Assert.Equal(HttpStatusCode.Created, own.StatusCode);

            await AssertProblemAsync(await AsEmsAsync(HttpMethod.Post, "/requisitions", StaffHrRoles.RequisitionsSubmit,
                new { departmentCode = "SEC", roleTitle = "Constable", positions = 1 }), HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Admin_IsNeverScoped()
        {
            var result = await ReadAsync<PagedResult<StaffSummaryDto>>(
                await AsEmsAsync(HttpMethod.Get, "/staff?pageSize=100", roles: StaffHrRoles.Admin));

            Assert.Contains(result.Items, s => s.HomeDepartment.Code == "SEC");
        }

        [Fact]
        public async Task EmailUniqueness_StaysCityWide()
        {
            // HR admin (unscoped) creates the employee; a scoped read cannot see SEC staff, but the
            // duplicate-email check must still look across every department.
            var request = NewStaffRequest() with { Email = "celeste.varga@cmes.example" };
            await AssertProblemAsync(await PostAsync("/staff", request), HttpStatusCode.Conflict);
        }
    }
}
