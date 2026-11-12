/**
 * @file AuditApiTests.cs
 * @brief Regression tests for audit logging and GET /audit.
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Every create, update and delete leaves an AuditLog row; failed requests leave none.</summary>
    public class AuditApiTests : ApiTestBase
    {
        /// <summary>Creates the test class.</summary>
        /// <param name="factory">The shared app.</param>
        public AuditApiTests(StaffHrApiFactory factory) : base(factory)
        {
        }

        private Task<PagedResult<AuditLogDto>> AuditAsync(string entityType, string entityId) =>
            GetAsync<PagedResult<AuditLogDto>>($"/audit?entityType={entityType}&entityId={Uri.EscapeDataString(entityId)}");

        [Fact]
        public async Task CreatingStaff_IsAuditedWithTheStaffNumber()
        {
            var staff = await CreateStaffAsync();

            var audit = Assert.Single((await AuditAsync("Staff", staff.StaffNumber)).Items);

            Assert.Equal("Created", audit.Action);
            Assert.Equal(StaffHrApiFactory.Now.UtcDateTime, audit.TimestampUtc);
            Assert.False(string.IsNullOrEmpty(audit.ActorId));
        }

        [Fact]
        public async Task UpdatingStaff_AddsAnUpdatedRow_NewestFirst()
        {
            var staff = await CreateStaffAsync();
            await PatchAsync($"/staff/{staff.StaffNumber}", new { phone = "555-0188" });

            var audit = (await AuditAsync("Staff", staff.StaffNumber)).Items;

            Assert.Equal(["Updated", "Created"], audit.Select(a => a.Action));
            Assert.True(audit[0].AuditId > audit[1].AuditId);
        }

        [Fact]
        public async Task InitialAssignment_IsAuditedWithItsNewId()
        {
            var serviceId = await ServiceIdAsync("HRS", "Recruitment");
            var staff = await CreateStaffAsync(new CreateAssignmentRequest(serviceId, "Recruiter", null, null));
            var assignmentId = Assert.Single(staff.Assignments).AssignmentId;

            var audit = Assert.Single((await AuditAsync("StaffAssignment", assignmentId.ToString())).Items);
            Assert.Equal("Created", audit.Action);
        }

        [Fact]
        public async Task DeletingAShift_IsAudited()
        {
            var staff = await CreateStaffAsync();
            var serviceId = await ServiceIdAsync("HRS", "Training & Development");
            var entry = await ReadAsync<RosterEntryDto>(await PostAsync("/rosters/entries", new
            {
                staffNumber = staff.StaffNumber, serviceId, shiftType = "Day",
                shiftStart = "2026-12-20T08:00:00Z", shiftEnd = "2026-12-20T16:00:00Z",
            }), HttpStatusCode.Created);

            await Client.DeleteAsync($"/rosters/entries/{entry.RosterEntryId}");

            var audit = (await AuditAsync("RosterEntry", entry.RosterEntryId.ToString())).Items;
            Assert.Equal(["Deleted", "Created"], audit.Select(a => a.Action));
        }

        [Fact]
        public async Task Requisitions_AreAuditedByNumber()
        {
            var requisition = await ReadAsync<RequisitionDto>(
                await PostAsync("/requisitions", new { departmentCode = "HRS", roleTitle = "Auditor", positions = 1 }, Guid.NewGuid().ToString()),
                HttpStatusCode.Created);
            await PatchAsync($"/requisitions/{requisition.RequisitionNumber}", new { status = "HRReview" });

            var audit = (await AuditAsync("Requisition", requisition.RequisitionNumber)).Items;
            Assert.Equal(["Updated", "Created"], audit.Select(a => a.Action));
        }

        [Fact]
        public async Task RejectedRequest_WritesNoAuditRow()
        {
            var staff = await CreateStaffAsync();

            await PatchAsync($"/staff/{staff.StaffNumber}", new { email = "talia.brennwood@cmes.example" }); // 409

            Assert.Single((await AuditAsync("Staff", staff.StaffNumber)).Items);
        }

        [Fact]
        public async Task SeededRecords_AreAuditedAsCreatedBySystem()
        {
            var audit = Assert.Single((await AuditAsync("Staff", Seed.Desmond)).Items);
            Assert.Equal("Created", audit.Action);
            Assert.Equal("system", audit.ActorId);
        }

        [Fact]
        public async Task Search_IsPaged()
        {
            var page = await GetAsync<PagedResult<AuditLogDto>>("/audit?pageSize=5");

            Assert.Equal(5, page.Items.Count);
            Assert.True(page.Total > 5);
            Assert.Equal(page.Items.OrderByDescending(a => a.AuditId).Select(a => a.AuditId), page.Items.Select(a => a.AuditId));
        }

        [Theory]
        [InlineData("page=0", "page")]
        [InlineData("pageSize=101", "pageSize")]
        public async Task Search_InvalidPaging_Returns422(string query, string field)
        {
            await AssertProblemAsync(await Client.GetAsync($"/audit?{query}"), HttpStatusCode.UnprocessableEntity, field);
        }
    }
}
