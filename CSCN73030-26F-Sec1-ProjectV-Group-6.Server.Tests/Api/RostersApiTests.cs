/**
 * @file RostersApiTests.cs
 * @brief Regression tests for /rosters (business rule 6: shift ordering and no overlaps).
 */

using System.Net;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Api
{
    /// <summary>
    /// Duty roster. The seed rosters the Paramedic Service from 4 to 10 November 2026 in 12-hour
    /// shifts (07:00 and 19:00 UTC); at the test instant (5 Nov, 12:00) Desmond and Priya are on duty.
    /// Write tests roster new HRS staff in December so seeded counts never change.
    /// </summary>
    public class RostersApiTests(StaffHrApiFactory factory) : ApiTestBase(factory)
    {
        private static DateTime Utc(int month, int day, int hour) => new(2026, month, day, hour, 0, 0, DateTimeKind.Utc);

        private async Task<(string StaffNumber, int ServiceId)> NewStaffAndServiceAsync() =>
            ((await CreateStaffAsync()).StaffNumber, await ServiceIdAsync("HRS", "Training & Development"));

        private Task<HttpResponseMessage> AddShiftAsync(string staffNumber, int serviceId, DateTime start, DateTime end,
            string shiftType = "Day", string? status = null) =>
            PostAsync("/rosters/entries", new { staffNumber, serviceId, shiftStart = start, shiftEnd = end, shiftType, status });

        // ---- GET /rosters ------------------------------------------------------------------------------

        [Fact]
        public async Task List_DefaultsToTheSevenDaysFromMidnightToday()
        {
            var entries = await GetAsync<List<RosterEntryDto>>($"/rosters?service={Seed.ParamedicServiceId}");

            // 28 seeded shifts minus the two day shifts on 4 Nov, which end before midnight on the 5th.
            Assert.Equal(26, entries.Count);
            Assert.Equal(entries.OrderBy(e => e.ShiftStart).Select(e => e.RosterEntryId), entries.Select(e => e.RosterEntryId));
            Assert.All(entries, e => Assert.True(e.ShiftEnd > Utc(11, 5, 0) && e.ShiftStart < Utc(11, 12, 0)));
            Assert.All(entries, e => Assert.Equal(DateTimeKind.Utc, e.ShiftStart.Kind));
        }

        [Fact]
        public async Task List_ExplicitWindowReturnsOverlappingShifts()
        {
            var entries = await GetAsync<List<RosterEntryDto>>(
                $"/rosters?service={Seed.ParamedicServiceId}&from=2026-11-05T12:00:00Z&to=2026-11-05T13:00:00Z");

            Assert.Equal([Seed.Desmond, Seed.Priya], entries.Select(e => e.StaffNumber).Order());
        }

        [Fact]
        public async Task List_SeedShowsCompletedConfirmedScheduledAndCancelledShifts()
        {
            var entries = await GetAsync<List<RosterEntryDto>>(
                $"/rosters?service={Seed.ParamedicServiceId}&from=2026-11-04T00:00:00Z&to=2026-11-12T00:00:00Z");

            Assert.Equal(28, entries.Count);
            Assert.All(entries.Where(e => e.ShiftEnd <= StaffHrApiFactory.Now.UtcDateTime),
                e => Assert.Equal(RosterStatus.Completed, e.Status));
            Assert.Contains(entries, e => e.Status == RosterStatus.Confirmed);
            Assert.Contains(entries, e => e.Status == RosterStatus.Scheduled);
            var cancelled = Assert.Single(entries, e => e.Status == RosterStatus.Cancelled);
            Assert.Equal(Seed.Kofi, cancelled.StaffNumber);
        }

        [Fact]
        public async Task List_ToNotAfterFrom_Returns422()
        {
            await AssertProblemAsync(await Client.GetAsync("/rosters?from=2026-11-05&to=2026-11-01"),
                HttpStatusCode.UnprocessableEntity, "to");
        }

        [Fact]
        public async Task List_WindowLongerThan62Days_Returns422()
        {
            await AssertProblemAsync(await Client.GetAsync("/rosters?from=2026-01-01&to=2026-06-01"),
                HttpStatusCode.UnprocessableEntity, "to");
        }

        [Fact]
        public async Task List_UnknownService_Returns422()
        {
            await AssertProblemAsync(await Client.GetAsync("/rosters?service=9999"), HttpStatusCode.UnprocessableEntity, "service");
        }

        [Fact]
        public async Task List_UnreadableDate_Returns400()
        {
            await AssertProblemAsync(await Client.GetAsync("/rosters?from=tomorrow"), HttpStatusCode.BadRequest, "from");
        }

        // ---- GET /rosters/on-duty ---------------------------------------------------------------------------

        [Fact]
        public async Task OnDuty_DefaultsToNow()
        {
            var entries = await GetAsync<List<RosterEntryDto>>($"/rosters/on-duty?service={Seed.ParamedicServiceId}");
            Assert.Equal([Seed.Desmond, Seed.Priya], entries.Select(e => e.StaffNumber).Order());
            Assert.All(entries, e => Assert.Equal(ShiftType.Day, e.ShiftType));
        }

        [Fact]
        public async Task OnDuty_AtAGivenInstant()
        {
            var entries = await GetAsync<List<RosterEntryDto>>("/rosters/on-duty?at=2026-11-05T20:00:00Z");
            Assert.Equal([Seed.Talia, Seed.Ines], entries.Select(e => e.StaffNumber).Order());
            Assert.All(entries, e => Assert.Equal(ShiftType.Night, e.ShiftType));
        }

        [Fact]
        public async Task OnDuty_ShiftEndIsExclusive()
        {
            // The day shift ends at 19:00, when the night shift starts.
            var entries = await GetAsync<List<RosterEntryDto>>($"/rosters/on-duty?service={Seed.ParamedicServiceId}&at=2026-11-05T19:00:00Z");
            Assert.Equal([Seed.Talia, Seed.Ines], entries.Select(e => e.StaffNumber).Order());
        }

        [Fact]
        public async Task OnDuty_ExcludesCancelledShifts()
        {
            // 9 Nov day shift: Jonah and Kofi, but Kofi's shift is cancelled.
            var entries = await GetAsync<List<RosterEntryDto>>($"/rosters/on-duty?service={Seed.ParamedicServiceId}&at=2026-11-09T12:00:00Z");
            Assert.Equal(Seed.Jonah, Assert.Single(entries).StaffNumber);
        }

        [Fact]
        public async Task OnDuty_UnknownService_Returns422()
        {
            await AssertProblemAsync(await Client.GetAsync("/rosters/on-duty?service=9999"), HttpStatusCode.UnprocessableEntity, "service");
        }

        // ---- GET /rosters/headcount ---------------------------------------------------------------------------

        [Fact]
        public async Task Headcount_ForOneService()
        {
            var rows = await GetAsync<List<ServiceHeadcountDto>>($"/rosters/headcount?service={Seed.ParamedicServiceId}");

            Assert.Equal(new ServiceHeadcountDto(Seed.ParamedicServiceId, "Paramedic Service", "EMS", 6, 2), Assert.Single(rows));
        }

        [Fact]
        public async Task Headcount_ForEveryService()
        {
            var rows = await GetAsync<List<ServiceHeadcountDto>>("/rosters/headcount");

            Assert.Equal(Seed.ServiceCount, rows.Count);
            // Among seeded services only the Paramedic Service is rostered at 12:00 on the test day.
            // (HRS is excluded: write tests roster their own HRS staff.)
            Assert.Equal(2, rows.Where(r => r.DepartmentCode != "HRS").Sum(r => r.OnDuty));
            // Rowan (OnLeave) does not count as active in Fire & Rescue.
            Assert.Equal(0, rows.Single(r => r.ServiceName == "Fire & Rescue").Active);
        }

        [Fact]
        public async Task Headcount_UnknownService_Returns422()
        {
            await AssertProblemAsync(await Client.GetAsync("/rosters/headcount?service=9999"), HttpStatusCode.UnprocessableEntity, "service");
        }

        // ---- POST /rosters/entries ---------------------------------------------------------------------------

        [Fact]
        public async Task Create_ValidShift_Returns201AsScheduled()
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();

            var response = await AddShiftAsync(staffNumber, serviceId, Utc(12, 1, 8), Utc(12, 1, 16));
            var entry = await ReadAsync<RosterEntryDto>(response, HttpStatusCode.Created);

            Assert.Equal($"/rosters/entries/{entry.RosterEntryId}", response.Headers.Location?.OriginalString);
            Assert.Equal(staffNumber, entry.StaffNumber);
            Assert.Equal("Training & Development", entry.ServiceName);
            Assert.Equal(RosterStatus.Scheduled, entry.Status);
            Assert.Equal(Utc(12, 1, 8), entry.ShiftStart);
        }

        [Fact]
        public async Task Create_TimeWithOffset_IsStoredAsUtc()
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();

            var response = await PostAsync("/rosters/entries",
                $"{{\"staffNumber\":\"{staffNumber}\",\"serviceId\":{serviceId},\"shiftStart\":\"2026-12-02T07:00:00-05:00\",\"shiftEnd\":\"2026-12-02T15:00:00-05:00\",\"shiftType\":\"Day\"}}");
            var entry = await ReadAsync<RosterEntryDto>(response, HttpStatusCode.Created);

            Assert.Equal(Utc(12, 2, 12), entry.ShiftStart);
            Assert.Equal(Utc(12, 2, 20), entry.ShiftEnd);
        }

        [Fact]
        public async Task Create_TimeWithoutOffset_IsReadAsUtc()
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();

            var response = await PostAsync("/rosters/entries",
                $"{{\"staffNumber\":\"{staffNumber}\",\"serviceId\":{serviceId},\"shiftStart\":\"2026-12-03T07:00:00\",\"shiftEnd\":\"2026-12-03T15:00:00\",\"shiftType\":\"Day\"}}");
            var entry = await ReadAsync<RosterEntryDto>(response, HttpStatusCode.Created);

            Assert.Equal(Utc(12, 3, 7), entry.ShiftStart);
        }

        [Fact]
        public async Task Create_OverlappingShift_Returns409AndAddsNothing()
        {
            var before = await GetAsync<List<RosterEntryDto>>($"/rosters?service={Seed.ParamedicServiceId}");

            var problem = await AssertProblemAsync(
                await AddShiftAsync(Seed.Desmond, Seed.ParamedicServiceId, Utc(11, 5, 10), Utc(11, 5, 14)),
                HttpStatusCode.Conflict);

            Assert.Contains(Seed.Desmond, problem.GetProperty("detail").GetString());
            var after = await GetAsync<List<RosterEntryDto>>($"/rosters?service={Seed.ParamedicServiceId}");
            Assert.Equal(before.Count, after.Count);
        }

        [Theory]
        [InlineData(6, 9)]    // starts before, ends inside
        [InlineData(12, 18)]  // starts inside, ends after
        [InlineData(9, 10)]   // entirely inside
        [InlineData(6, 18)]   // covers it completely
        public async Task Create_AnyKindOfOverlap_Returns409(int startHour, int endHour)
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();
            await ReadAsync<RosterEntryDto>(await AddShiftAsync(staffNumber, serviceId, Utc(12, 4, 8), Utc(12, 4, 16)), HttpStatusCode.Created);

            await AssertProblemAsync(
                await AddShiftAsync(staffNumber, serviceId, Utc(12, 4, startHour), Utc(12, 4, endHour)),
                HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_BackToBackShifts_AreAllowed()
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();
            await ReadAsync<RosterEntryDto>(await AddShiftAsync(staffNumber, serviceId, Utc(12, 5, 8), Utc(12, 5, 16)), HttpStatusCode.Created);

            await ReadAsync<RosterEntryDto>(await AddShiftAsync(staffNumber, serviceId, Utc(12, 5, 16), Utc(12, 5, 20)), HttpStatusCode.Created);
            await ReadAsync<RosterEntryDto>(await AddShiftAsync(staffNumber, serviceId, Utc(12, 5, 4), Utc(12, 5, 8)), HttpStatusCode.Created);
        }

        [Fact]
        public async Task Create_CancelledShiftDoesNotBlockANewOne()
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();
            await ReadAsync<RosterEntryDto>(
                await AddShiftAsync(staffNumber, serviceId, Utc(12, 6, 8), Utc(12, 6, 16), status: "Cancelled"), HttpStatusCode.Created);

            await ReadAsync<RosterEntryDto>(await AddShiftAsync(staffNumber, serviceId, Utc(12, 6, 8), Utc(12, 6, 16)), HttpStatusCode.Created);
        }

        [Fact]
        public async Task Create_OverlapForADifferentPerson_IsAllowed()
        {
            var (first, serviceId) = await NewStaffAndServiceAsync();
            var (second, _) = await NewStaffAndServiceAsync();
            await ReadAsync<RosterEntryDto>(await AddShiftAsync(first, serviceId, Utc(12, 7, 8), Utc(12, 7, 16)), HttpStatusCode.Created);

            await ReadAsync<RosterEntryDto>(await AddShiftAsync(second, serviceId, Utc(12, 7, 8), Utc(12, 7, 16)), HttpStatusCode.Created);
        }

        [Theory]
        [InlineData(16, 8)]  // end before start
        [InlineData(8, 8)]   // zero length
        public async Task Create_EndNotAfterStart_Returns422(int startHour, int endHour)
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();
            await AssertProblemAsync(await AddShiftAsync(staffNumber, serviceId, Utc(12, 8, startHour), Utc(12, 8, endHour)),
                HttpStatusCode.UnprocessableEntity, "shiftEnd");
        }

        [Fact]
        public async Task Create_ShiftLongerThan24Hours_Returns422()
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();
            await AssertProblemAsync(await AddShiftAsync(staffNumber, serviceId, Utc(12, 9, 8), Utc(12, 10, 9)),
                HttpStatusCode.UnprocessableEntity, "shiftEnd");
        }

        [Fact]
        public async Task Create_MissingFields_Returns422()
        {
            await AssertProblemAsync(await PostAsync("/rosters/entries", new { }), HttpStatusCode.UnprocessableEntity,
                "staffNumber", "serviceId", "shiftStart", "shiftEnd", "shiftType");
        }

        [Fact]
        public async Task Create_UnknownStaffOrService_Returns422()
        {
            await AssertProblemAsync(await AddShiftAsync("STF-9999999", 9999, Utc(12, 11, 8), Utc(12, 11, 16)),
                HttpStatusCode.UnprocessableEntity, "staffNumber", "serviceId");
        }

        [Fact]
        public async Task Create_TerminatedEmployee_Returns422()
        {
            var serviceId = await ServiceIdAsync("PTR", "Transit Fleet Maintenance");
            await AssertProblemAsync(await AddShiftAsync(Seed.Matteo, serviceId, Utc(12, 12, 8), Utc(12, 12, 16)),
                HttpStatusCode.UnprocessableEntity, "staffNumber");
        }

        [Fact]
        public async Task Create_UnknownShiftType_Returns400()
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();
            await AssertProblemAsync(await AddShiftAsync(staffNumber, serviceId, Utc(12, 13, 8), Utc(12, 13, 16), shiftType: "Brunch"),
                HttpStatusCode.BadRequest, "$.shiftType");
        }

        // ---- DELETE /rosters/entries/{id} ---------------------------------------------------------------------

        [Fact]
        public async Task Delete_RemovesTheShift_ThenReturns404()
        {
            var (staffNumber, serviceId) = await NewStaffAndServiceAsync();
            var entry = await ReadAsync<RosterEntryDto>(
                await AddShiftAsync(staffNumber, serviceId, Utc(12, 14, 8), Utc(12, 14, 16)), HttpStatusCode.Created);

            var deleted = await Client.DeleteAsync($"/rosters/entries/{entry.RosterEntryId}");
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            await AssertProblemAsync(await Client.DeleteAsync($"/rosters/entries/{entry.RosterEntryId}"), HttpStatusCode.NotFound);

            // The slot is free again.
            await ReadAsync<RosterEntryDto>(await AddShiftAsync(staffNumber, serviceId, Utc(12, 14, 8), Utc(12, 14, 16)), HttpStatusCode.Created);
        }
    }
}
