/**
 * @file AssignmentsApiTests.cs
 * @brief Regression tests for assignment endpoints (business rule 6: StartDate before EndDate).
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>POST/GET /staff/{staffNumber}/assignments and PATCH /assignments/{id}.</summary>
    public class AssignmentsApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        private static readonly DateOnly Today = StaffHrApiFactory.Today;

        private async Task<(StaffProfileDto Staff, int ServiceId)> NewStaffAndServiceAsync() =>
            (await CreateStaffAsync(), await ServiceIdAsync("HRS", "Employee Relations"));

        [Fact]
        public async Task Create_ValidRequest_Returns201()
        {
            var (staff, serviceId) = await NewStaffAndServiceAsync();

            var assignment = await ReadAsync<AssignmentDto>(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments",
                    new CreateAssignmentRequest(serviceId, "  Employee Relations Advisor  ", Today.AddDays(-10), Today.AddDays(90))),
                HttpStatusCode.Created);

            Assert.Equal(staff.StaffNumber, assignment.StaffNumber);
            Assert.Equal("Employee Relations Advisor", assignment.RoleTitle); // trimmed
            Assert.Equal("Employee Relations", assignment.ServiceName);
            Assert.Equal("HRS", assignment.DepartmentCode);
            Assert.Equal(Today.AddDays(90), assignment.EndDate);
            Assert.True(assignment.IsCurrent);
        }

        [Fact]
        public async Task Create_WithoutStartDate_StartsToday()
        {
            var (staff, serviceId) = await NewStaffAndServiceAsync();

            var assignment = await ReadAsync<AssignmentDto>(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments", new { serviceId, roleTitle = "Advisor" }),
                HttpStatusCode.Created);

            Assert.Equal(Today, assignment.StartDate);
            Assert.Null(assignment.EndDate);
        }

        [Fact]
        public async Task Create_FutureAssignment_IsNotCurrent()
        {
            var (staff, serviceId) = await NewStaffAndServiceAsync();

            var assignment = await ReadAsync<AssignmentDto>(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments", new CreateAssignmentRequest(serviceId, "Advisor", Today.AddDays(5), null)),
                HttpStatusCode.Created);

            Assert.False(assignment.IsCurrent);
        }

        [Theory]
        [InlineData(0)]   // same day
        [InlineData(-1)]  // before
        public async Task Create_EndDateNotAfterStartDate_Returns422(int endOffset)
        {
            var (staff, serviceId) = await NewStaffAndServiceAsync();

            await AssertProblemAsync(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments",
                    new CreateAssignmentRequest(serviceId, "Advisor", Today, Today.AddDays(endOffset))),
                HttpStatusCode.UnprocessableEntity, "endDate");
        }

        [Fact]
        public async Task Create_MissingFields_Returns422()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(await PostAsync($"/staff/{staff.StaffNumber}/assignments", new { }),
                HttpStatusCode.UnprocessableEntity, "serviceId", "roleTitle");
        }

        [Fact]
        public async Task Create_UnknownService_Returns422()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments", new { serviceId = 9999, roleTitle = "Advisor" }),
                HttpStatusCode.UnprocessableEntity, "serviceId");
        }

        [Fact]
        public async Task Create_ForTerminatedEmployee_Returns422()
        {
            var serviceId = await ServiceIdAsync("PTR", "Transit Fleet Maintenance");
            await AssertProblemAsync(
                await PostAsync($"/staff/{Seed.Matteo}/assignments", new { serviceId, roleTitle = "Fleet Mechanic" }),
                HttpStatusCode.UnprocessableEntity, "staffNumber");

            var after = await GetAsync<List<AssignmentDto>>($"/staff/{Seed.Matteo}/assignments");
            Assert.Single(after);
        }

        [Fact]
        public async Task Create_UnknownStaff_Returns404()
        {
            await AssertProblemAsync(
                await PostAsync("/staff/STF-9999999/assignments", new { serviceId = 1, roleTitle = "X" }),
                HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_MalformedBody_Returns400()
        {
            var staff = await CreateStaffAsync();
            await AssertProblemAsync(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments", "{\"startDate\": \"tomorrow\"}"),
                HttpStatusCode.BadRequest, "$.startDate");
        }

        [Fact]
        public async Task List_ReturnsAllAssignmentsNewestFirst()
        {
            var assignments = await GetAsync<List<AssignmentDto>>($"/staff/{Seed.Desmond}/assignments");

            Assert.Equal(2, assignments.Count);
            Assert.True(assignments[0].StartDate > assignments[1].StartDate);
            Assert.True(assignments[0].IsCurrent);
            Assert.False(assignments[1].IsCurrent);
            Assert.Equal(assignments[0].StartDate.AddDays(-1), assignments[1].EndDate);
        }

        [Fact]
        public async Task List_UnknownStaff_Returns404()
        {
            await AssertProblemAsync(await Client.GetAsync("/staff/STF-9999999/assignments"), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Patch_SetsEndDateToCloseTheAssignment()
        {
            var (staff, serviceId) = await NewStaffAndServiceAsync();
            var created = await ReadAsync<AssignmentDto>(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments", new CreateAssignmentRequest(serviceId, "Advisor", Today.AddDays(-30), null)),
                HttpStatusCode.Created);

            var closed = await ReadAsync<AssignmentDto>(
                await PatchAsync($"/assignments/{created.AssignmentId}", new { endDate = Today.AddDays(-1) }));

            Assert.Equal(Today.AddDays(-1), closed.EndDate);
            Assert.False(closed.IsCurrent);

            var profile = await GetAsync<StaffProfileDto>($"/staff/{staff.StaffNumber}");
            Assert.Equal(Today.AddDays(-1), Assert.Single(profile.Assignments).EndDate);
        }

        [Fact]
        public async Task Patch_EndDateNotAfterStartDate_Returns422()
        {
            var (staff, serviceId) = await NewStaffAndServiceAsync();
            var created = await ReadAsync<AssignmentDto>(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments", new CreateAssignmentRequest(serviceId, "Advisor", Today, null)),
                HttpStatusCode.Created);

            await AssertProblemAsync(await PatchAsync($"/assignments/{created.AssignmentId}", new { endDate = Today }),
                HttpStatusCode.UnprocessableEntity, "endDate");
        }

        [Fact]
        public async Task Patch_WithoutEndDate_Returns422()
        {
            var (staff, serviceId) = await NewStaffAndServiceAsync();
            var created = await ReadAsync<AssignmentDto>(
                await PostAsync($"/staff/{staff.StaffNumber}/assignments", new CreateAssignmentRequest(serviceId, "Advisor", Today, null)),
                HttpStatusCode.Created);

            await AssertProblemAsync(await PatchAsync($"/assignments/{created.AssignmentId}", new { }),
                HttpStatusCode.UnprocessableEntity, "endDate");
        }

        [Fact]
        public async Task Patch_UnknownAssignment_Returns404()
        {
            await AssertProblemAsync(await PatchAsync("/assignments/999999", new { endDate = Today }), HttpStatusCode.NotFound);
        }
    }
}
