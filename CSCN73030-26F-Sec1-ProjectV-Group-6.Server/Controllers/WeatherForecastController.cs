using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers
{
    /// <summary>
    /// Sample API controller that serves random weather forecasts at <c>/weatherforecast</c>.
    /// </summary>
    /// <remarks>
    /// Template code; it shows the controller pattern the Staff/HR endpoints will follow.
    /// </remarks>
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        /// <summary>Descriptions a forecast's summary is picked from at random.</summary>
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        /// <summary>
        /// Returns a forecast for each of the next five days.
        /// </summary>
        /// <returns>
        /// Five forecasts, one per day starting tomorrow, each with a random temperature
        /// from -20 to 54 degrees Celsius and a random summary.
        /// </returns>
        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
