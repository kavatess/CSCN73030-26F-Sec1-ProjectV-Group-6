using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests
{
    // Boots the real application in memory and calls it over HTTP, so routing, JSON
    // serialization and middleware are covered as well as the controller itself.
    public class WeatherForecastApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public WeatherForecastApiTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        }

        // The React client and the Vite proxy both use the lower-case path.
        [Theory]
        [InlineData("/WeatherForecast")]
        [InlineData("/weatherforecast")]
        public async Task Get_ReturnsOkWithJson(string url)
        {
            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        // App.jsx reads exactly these camelCase fields; renaming one breaks the page.
        [Fact]
        public async Task Get_ReturnsFiveForecastsWithTheFieldsTheClientReads()
        {
            using var json = JsonDocument.Parse(await _client.GetStringAsync("/weatherforecast"));
            var forecasts = json.RootElement.EnumerateArray().ToList();

            Assert.Equal(5, forecasts.Count);
            Assert.All(forecasts, forecast =>
            {
                Assert.Equal(JsonValueKind.String, forecast.GetProperty("date").ValueKind);
                Assert.Equal(JsonValueKind.Number, forecast.GetProperty("temperatureC").ValueKind);
                Assert.Equal(JsonValueKind.Number, forecast.GetProperty("temperatureF").ValueKind);
                Assert.Equal(JsonValueKind.String, forecast.GetProperty("summary").ValueKind);
            });
        }

        // App.jsx uses the date as the React row key, so dates must be unique.
        [Fact]
        public async Task Get_ReturnsUniqueIsoDates()
        {
            using var json = JsonDocument.Parse(await _client.GetStringAsync("/weatherforecast"));
            var dates = json.RootElement.EnumerateArray()
                .Select(forecast => forecast.GetProperty("date").GetString()!)
                .ToList();

            Assert.Equal(dates.Count, dates.Distinct().Count());
            Assert.All(dates, date => Assert.True(DateOnly.TryParseExact(date, "yyyy-MM-dd", out _), date));
        }

        [Fact]
        public async Task Post_IsNotAllowed()
        {
            var response = await _client.PostAsync("/weatherforecast", content: null);

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
    }
}
