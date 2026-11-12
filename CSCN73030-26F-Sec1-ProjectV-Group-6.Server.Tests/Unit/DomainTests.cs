/**
 * @file DomainTests.cs
 * @brief Unit tests for small domain helpers: Clock, DisplayName, IsCurrentOn, API exceptions.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using Microsoft.Extensions.Time.Testing;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>Clock, Staff.DisplayName, StaffAssignment.IsCurrentOn and the API exception types.</summary>
    public class DomainTests
    {
        [Fact]
        public void Clock_ReadsUtcNowAndTodayFromTheTimeProvider()
        {
            var clock = new Clock(new FakeTimeProvider(new DateTimeOffset(2026, 11, 5, 23, 30, 0, TimeSpan.Zero)));

            Assert.Equal(DateTimeKind.Utc, clock.UtcNow.Kind);
            Assert.Equal(new DateOnly(2026, 11, 5), clock.Today);
        }

        [Fact]
        public void Clock_AsUtc_TreatsATimeWithoutOffsetAsUtc()
        {
            var unspecified = new DateTime(2026, 11, 5, 7, 0, 0, DateTimeKind.Unspecified);

            var utc = Clock.AsUtc(unspecified);

            Assert.Equal(DateTimeKind.Utc, utc.Kind);
            Assert.Equal(unspecified.Ticks, utc.Ticks);
        }

        [Fact]
        public void Clock_AsUtc_ConvertsLocalTimeAndKeepsUtc()
        {
            var utcValue = new DateTime(2026, 11, 5, 7, 0, 0, DateTimeKind.Utc);
            Assert.Equal(utcValue, Clock.AsUtc(utcValue));
            Assert.Equal(utcValue, Clock.AsUtc(utcValue.ToLocalTime()));
        }

        [Theory]
        [InlineData("Desmond", "Des", "Des Achterberg")]
        [InlineData("Desmond", null, "Desmond Achterberg")]
        public void Staff_DisplayName_PrefersThePreferredName(string first, string? preferred, string expected)
        {
            var staff = new Staff { FirstName = first, PreferredName = preferred, LastName = "Achterberg" };
            Assert.Equal(expected, staff.DisplayName);
        }

        [Theory]
        [InlineData("2026-11-01", null, "2026-11-05", true)]          // open-ended
        [InlineData("2026-11-05", null, "2026-11-05", true)]          // starts today
        [InlineData("2026-11-06", null, "2026-11-05", false)]         // starts tomorrow
        [InlineData("2026-01-01", "2026-11-05", "2026-11-05", true)]  // ends today (inclusive)
        [InlineData("2026-01-01", "2026-11-04", "2026-11-05", false)] // ended yesterday
        public void StaffAssignment_IsCurrentOn_IsInclusiveAtBothEnds(string start, string? end, string day, bool expected)
        {
            var assignment = new StaffAssignment
            {
                StartDate = DateOnly.Parse(start),
                EndDate = end == null ? null : DateOnly.Parse(end),
            };
            Assert.Equal(expected, assignment.IsCurrentOn(DateOnly.Parse(day)));
        }

        [Fact]
        public void ApiExceptions_CarryTheirStatusCodes()
        {
            Assert.Equal(400, new BadRequestException("x").StatusCode);
            Assert.Equal(404, new NotFoundException("Staff", "STF-0000099").StatusCode);
            Assert.Equal(409, new ConflictException("x").StatusCode);
            Assert.Equal(422, new ValidationFailedException("f", "m").StatusCode);
        }

        [Fact]
        public void NotFoundException_NamesTheResourceAndKey()
        {
            Assert.Equal("Staff 'STF-0000099' was not found.", new NotFoundException("Staff", "STF-0000099").Message);
        }
    }
}
