using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests
{
    public class WeatherForecastControllerTests
    {
        private readonly WeatherForecastController _controller = new();

        [Fact]
        public void Get_ReturnsFiveForecasts()
        {
            Assert.Equal(5, _controller.Get().Count());
        }

        [Fact]
        public void Get_ReturnsOneForecastPerDayStartingTomorrow()
        {
            var before = DateOnly.FromDateTime(DateTime.Now);
            var dates = _controller.Get().Select(forecast => forecast.Date).ToList();
            var after = DateOnly.FromDateTime(DateTime.Now);

            // before and after only differ if the test happens to straddle midnight.
            Assert.Contains(dates[0], new[] { before.AddDays(1), after.AddDays(1) });
            for (var i = 1; i < dates.Count; i++)
            {
                Assert.Equal(dates[i - 1].AddDays(1), dates[i]);
            }
        }

        [Fact]
        public void Get_TemperaturesStayWithinSupportedRange()
        {
            // Temperatures are random, so sample many calls.
            for (var run = 0; run < 100; run++)
            {
                Assert.All(_controller.Get(), forecast => Assert.InRange(forecast.TemperatureC, -20, 54));
            }
        }

        [Fact]
        public void Get_EveryForecastHasASummary()
        {
            Assert.All(_controller.Get(), forecast => Assert.False(string.IsNullOrWhiteSpace(forecast.Summary)));
        }
    }
}
