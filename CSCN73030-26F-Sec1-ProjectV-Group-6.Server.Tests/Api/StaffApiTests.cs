/**
 * @file StaffApiTests.cs
 * @brief Regression tests for POST, GET and PATCH /staff (business rules 1, 2 and 4).
 */

using System.Net;
using System.Text.Json;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>Onboarding, profile and partial update of employees.</summary>
    public class StaffApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        // ---- POST /staff ----------------------------------------------------------------------

        [Fact]
        public async Task Create_ValidRequest_Returns201WithLocationAndProfile()
        {
            var request = NewStaffRequest() with { PreferredName = "Ave", Email = UniqueEmail().ToUpperInvariant() };

            var response = await PostAsync("/staff", request);
            var profile = await ReadAsync<StaffProfileDto>(response, HttpStatusCode.Created);

            Assert.Equal($"/staff/{profile.StaffNumber}", response.Headers.Location?.AbsolutePath);
            Assert.Equal("Ave Employee", profile.DisplayName);
            Assert.Equal(request.Email!.ToLowerInvariant(), profile.Email);   // stored lower-case
            Assert.Equal(EmploymentStatus.Probation, profile.EmploymentStatus); // default for new hires
            Assert.Equal("HRS", profile.HomeDepartment.Code);
            Assert.Empty(profile.Assignments);
            Assert.Null(profile.CredentialState);
        }

        [Fact]
        public async Task Create_IssuesAWellFormedStaffNumber()
        {
            var profile = await CreateStaffAsync();
            Assert.True(StaffNumber.IsWellFormed(profile.StaffNumber), profile.StaffNumber);
        }

        [Fact]
        public async Task Create_StaffNumbersComeFromOneIncreasingSequence()
        {
            var first = await CreateStaffAsync();
            var second = await CreateStaffAsync();

            Assert.NotEqual(first.StaffNumber, second.StaffNumber);
            Assert.True(string.CompareOrdinal(second.StaffNumber, first.StaffNumber) > 0);
            // The 20 seeded employees hold STF-0000001 to STF-0000020, so new numbers never reuse them.
            Assert.True(string.CompareOrdinal(first.StaffNumber, "STF-0000020") > 0);
        }

        [Fact]
        public async Task Create_WithInitialAssignment_CreatesItInTheSameRequest()
        {
            var serviceId = await ServiceIdAsync("HRS", "Recruitment");

            var profile = await CreateStaffAsync(new CreateAssignmentRequest(serviceId, "Recruiter", null, null));

            var assignment = Assert.Single(profile.Assignments);
            Assert.Equal("Recruiter", assignment.RoleTitle);
            Assert.Equal(StaffHrApiFactory.Today, assignment.StartDate); // defaults to the hire date
            Assert.True(assignment.IsCurrent);
        }

        [Fact]
        public async Task Create_AcceptsActiveAsTheInitialStatus()
        {
            var request = NewStaffRequest() with { EmploymentStatus = EmploymentStatus.Active };
            var profile = await ReadAsync<StaffProfileDto>(await PostAsync("/staff", request), HttpStatusCode.Created);
            Assert.Equal(EmploymentStatus.Active, profile.EmploymentStatus);
        }

        [Fact]
        public async Task Create_DuplicateEmailInAnyCase_Returns409()
        {
            var existing = await CreateStaffAsync();
            var request = NewStaffRequest() with { Email = existing.Email.ToUpperInvariant() };

            var problem = await AssertProblemAsync(await PostAsync("/staff", request), HttpStatusCode.Conflict);
            Assert.Contains("already in use", problem.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Create_SeededEmail_Returns409()
        {
            var request = NewStaffRequest() with { Email = "desmond.achterberg@cmes.example" };
            await AssertProblemAsync(await PostAsync("/staff", request), HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_MissingRequiredFields_Returns422ListingEachField()
        {
            await AssertProblemAsync(await PostAsync("/staff", new { }), HttpStatusCode.UnprocessableEntity,
                "firstName", "lastName", "email", "dateOfBirth", "hireDate", "employmentType", "homeDepartmentCode");
        }

        [Fact]
        public async Task Create_InvalidValues_Returns422ForEachField()
        {
            var request = NewStaffRequest() with
            {
                Email = "not-an-email",
                DateOfBirth = StaffHrApiFactory.Today.AddDays(1),
                EmploymentStatus = EmploymentStatus.Terminated,
                HomeDepartmentCode = "XYZ",
                FirstName = new string('a', 101),
            };

            await AssertProblemAsync(await PostAsync("/staff", request), HttpStatusCode.UnprocessableEntity,
                "email", "dateOfBirth", "employmentStatus", "homeDepartmentCode", "firstName");
        }

        [Fact]
        public async Task Create_HireDateLessThan16YearsAfterBirth_Returns422()
        {
            var request = NewStaffRequest() with { DateOfBirth = StaffHrApiFactory.Today.AddYears(-15) };
            await AssertProblemAsync(await PostAsync("/staff", request), HttpStatusCode.UnprocessableEntity, "hireDate");
        }

        [Fact]
        public async Task Create_InvalidInitialAssignment_Returns422WithNestedFieldNames()
        {
            var request = NewStaffRequest() with
            {
                InitialAssignment = new CreateAssignmentRequest(9999, null, StaffHrApiFactory.Today, StaffHrApiFactory.Today),
            };

            await AssertProblemAsync(await PostAsync("/staff", request), HttpStatusCode.UnprocessableEntity,
                "initialAssignment.serviceId", "initialAssignment.roleTitle", "initialAssignment.endDate");
        }

        [Fact]
        public async Task Create_RejectedRequest_DoesNotCreateAnyone()
        {
            var email = UniqueEmail();
            await PostAsync("/staff", NewStaffRequest() with { Email = email, HomeDepartmentCode = "XYZ" });

            var search = await GetAsync<PagedResult<StaffSummaryDto>>($"/staff?q={email}");
            Assert.Equal(0, search.Total);
        }

        [Theory]
        [InlineData("{\"firstName\": \"A\", ")]                                     // unreadable JSON
        [InlineData("{\"hireDate\": \"not-a-date\"}")]                              // wrong type
        [InlineData("{\"employmentType\": \"Wizard\"}")]                            // unknown enum value
        [InlineData("[]")]                                                          // wrong shape
        public async Task Create_MalformedBody_Returns400(string body)
        {
            await AssertProblemAsync(await PostAsync("/staff", body), HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_EmptyBody_Returns400()
        {
            var response = await Client.PostAsync("/staff", new StringContent("", System.Text.Encoding.UTF8, "application/json"));
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WrongTypeInBody_NamesTheJsonPath()
        {
            await AssertProblemAsync(await PostAsync("/staff", "{\"hireDate\": \"not-a-date\"}"),
                HttpStatusCode.BadRequest, "$.hireDate");
        }

        // ---- GET /staff/{staffNumber} --------------------------------------------------------------

        [Fact]
        public async Task Get_SeededEmployee_ReturnsFullProfile()
        {
            var profile = await GetAsync<StaffProfileDto>($"/staff/{Seed.Desmond}");

            Assert.Equal("Des Achterberg", profile.DisplayName);
            Assert.Equal("desmond.achterberg@cmes.example", profile.Email);
            Assert.Equal("EMS", profile.HomeDepartment.Code);
            Assert.Equal(EmploymentStatus.Active, profile.EmploymentStatus);

            // Current assignment first, then the past one.
            Assert.Equal(["Advanced Care Paramedic", "Primary Care Paramedic"], profile.Assignments.Select(a => a.RoleTitle));
            Assert.True(profile.Assignments[0].IsCurrent);
            Assert.False(profile.Assignments[1].IsCurrent);

            // Credentials soonest expiry first; the CPR certificate expires in 12 days.
            Assert.Equal(["CPR & First Aid Certificate", "Advanced Care Paramedic Licence"],
                profile.Credentials.Select(c => c.CredentialType));
            Assert.Equal(CredentialStatus.Expiring, profile.Credentials[0].Status);
            Assert.Equal(12, profile.Credentials[0].DaysUntilExpiry);
            Assert.Equal(CredentialStatus.Expiring, profile.CredentialState);
        }

        [Fact]
        public async Task Get_NeverReturnsSalary()
        {
            var json = await Client.GetStringAsync($"/staff/{Seed.Desmond}");

            Assert.DoesNotContain("salary", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("payBand", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("compensation", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Get_UsesCamelCaseAndStringEnums()
        {
            using var json = JsonDocument.Parse(await Client.GetStringAsync($"/staff/{Seed.Desmond}"));
            var root = json.RootElement;

            Assert.Equal(Seed.Desmond, root.GetProperty("staffNumber").GetString());
            Assert.Equal("Active", root.GetProperty("employmentStatus").GetString());
            Assert.Equal("1987-02-04", root.GetProperty("dateOfBirth").GetString());
        }

        [Fact]
        public async Task Get_UnknownStaffNumber_Returns404()
        {
            var problem = await AssertProblemAsync(await Client.GetAsync("/staff/STF-9999999"), HttpStatusCode.NotFound);
            Assert.Contains("STF-9999999", problem.GetProperty("detail").GetString());
        }

        // ---- PATCH /staff/{staffNumber} --------------------------------------------------------------

        [Fact]
        public async Task Patch_ChangesOnlyTheFieldsSent()
        {
            var created = await CreateStaffAsync();

            var updated = await ReadAsync<StaffProfileDto>(
                await PatchAsync($"/staff/{created.StaffNumber}", new { preferredName = "Teddy", phone = "555-0177" }));

            Assert.Equal("Teddy", updated.PreferredName);
            Assert.Equal("555-0177", updated.Phone);
            Assert.Equal(created.FirstName, updated.FirstName);
            Assert.Equal(created.Email, updated.Email);
            Assert.Equal(created.EmploymentStatus, updated.EmploymentStatus);
        }

        [Fact]
        public async Task Patch_EmptyStringClearsOptionalFields()
        {
            var created = await CreateStaffAsync();
            await PatchAsync($"/staff/{created.StaffNumber}", new { preferredName = "Teddy" });

            var updated = await ReadAsync<StaffProfileDto>(
                await PatchAsync($"/staff/{created.StaffNumber}", new { preferredName = "", phone = "" }));

            Assert.Null(updated.PreferredName);
            Assert.Null(updated.Phone);
        }

        [Fact]
        public async Task Patch_DifferentStaffNumber_Returns422AndKeepsTheNumber()
        {
            var created = await CreateStaffAsync();

            await AssertProblemAsync(
                await PatchAsync($"/staff/{created.StaffNumber}", new { staffNumber = "STF-0999999", firstName = "Changed" }),
                HttpStatusCode.UnprocessableEntity, "staffNumber");

            var after = await GetAsync<StaffProfileDto>($"/staff/{created.StaffNumber}");
            Assert.Equal("Test", after.FirstName); // nothing in the rejected request was applied
        }

        [Fact]
        public async Task Patch_SameStaffNumber_IsAccepted()
        {
            var created = await CreateStaffAsync();
            var response = await PatchAsync($"/staff/{created.StaffNumber}", new { staffNumber = created.StaffNumber, lastName = "Renamed" });
            Assert.Equal("Renamed", (await ReadAsync<StaffProfileDto>(response)).LastName);
        }

        [Fact]
        public async Task Patch_TransferToAnotherDepartment_KeepsTheStaffNumber()
        {
            var created = await CreateStaffAsync();

            var moved = await ReadAsync<StaffProfileDto>(
                await PatchAsync($"/staff/{created.StaffNumber}", new { homeDepartmentCode = "res" }));

            Assert.Equal("RES", moved.HomeDepartment.Code);
            Assert.Equal(created.StaffNumber, moved.StaffNumber);
            // Move back so RES headcount stays as seeded for other tests.
            await PatchAsync($"/staff/{created.StaffNumber}", new { homeDepartmentCode = "HRS" });
        }

        [Fact]
        public async Task Patch_EmailAlreadyUsed_Returns409()
        {
            var created = await CreateStaffAsync();
            await AssertProblemAsync(
                await PatchAsync($"/staff/{created.StaffNumber}", new { email = "Talia.Brennwood@cmes.example" }),
                HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Patch_OwnEmailInAnotherCase_IsAccepted()
        {
            var created = await CreateStaffAsync();
            var updated = await ReadAsync<StaffProfileDto>(
                await PatchAsync($"/staff/{created.StaffNumber}", new { email = created.Email.ToUpperInvariant() }));
            Assert.Equal(created.Email, updated.Email);
        }

        [Fact]
        public async Task Patch_ToTerminated_Returns422()
        {
            var created = await CreateStaffAsync();
            await AssertProblemAsync(
                await PatchAsync($"/staff/{created.StaffNumber}", new { employmentStatus = "Terminated" }),
                HttpStatusCode.UnprocessableEntity, "employmentStatus");
        }

        [Fact]
        public async Task Patch_InvalidValues_Returns422()
        {
            var created = await CreateStaffAsync();
            await AssertProblemAsync(
                await PatchAsync($"/staff/{created.StaffNumber}", new { firstName = " ", email = "bad", homeDepartmentCode = "XYZ" }),
                HttpStatusCode.UnprocessableEntity, "firstName", "email", "homeDepartmentCode");
        }

        [Fact]
        public async Task Patch_UnknownStaffNumber_Returns404()
        {
            await AssertProblemAsync(await PatchAsync("/staff/STF-9999999", new { firstName = "X" }), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Patch_MalformedBody_Returns400()
        {
            await AssertProblemAsync(await PatchAsync($"/staff/{Seed.Desmond}", "{\"employmentStatus\": \"Retired\"}"),
                HttpStatusCode.BadRequest);
        }

        // ---- Business rule 1 at the database layer -------------------------------------------------

        [Fact]
        public async Task StaffNumber_CannotBeChangedEvenThroughEfCore()
        {
            var created = await CreateStaffAsync();
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CmesDbContext>();
            var staff = await db.Staff.SingleAsync(s => s.StaffNumber == created.StaffNumber);

            staff.StaffNumber = "STF-0999999";

            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
    }
}
