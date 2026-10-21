namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests
{
    public class WeatherForecastTests
    {
        [Fact]
        public void TemperatureF_AtFreezingPoint_Is32()
        {
            var forecast = new WeatherForecast { TemperatureC = 0 };

            Assert.Equal(32, forecast.TemperatureF);
        }

        // The conversion truncates, so it may sit up to one degree below the exact value.
        [Theory]
        [InlineData(-20)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(54)]
        public void TemperatureF_IsWithinOneDegreeOfExactConversion(int celsius)
        {
            var forecast = new WeatherForecast { TemperatureC = celsius };
            var exact = celsius * 9.0 / 5.0 + 32;

            Assert.InRange((double)forecast.TemperatureF, exact - 1, exact + 1);
        }

        [Fact]
        public void TemperatureF_RisesWithTemperatureC()
        {
            for (var celsius = -20; celsius < 54; celsius++)
            {
                var colder = new WeatherForecast { TemperatureC = celsius };
                var warmer = new WeatherForecast { TemperatureC = celsius + 1 };

                Assert.True(warmer.TemperatureF > colder.TemperatureF, $"{celsius}C -> {celsius + 1}C");
            }
        }

        [Fact]
        public void NewForecast_HasNoSummary()
        {
            Assert.Null(new WeatherForecast().Summary);
        }
    }
}
