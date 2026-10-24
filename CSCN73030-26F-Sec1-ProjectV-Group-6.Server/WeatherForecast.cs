namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server
{
    /// <summary>
    /// One day's forecast, as returned by <c>GET /weatherforecast</c>.
    /// </summary>
    /// <remarks>
    /// Sample model from the project template; it will be replaced by the Staff/HR domain models.
    /// </remarks>
    public class WeatherForecast
    {
        /// <summary>Day the forecast applies to. Serialized as <c>yyyy-MM-dd</c>.</summary>
        public DateOnly Date { get; set; }

        /// <summary>Temperature in degrees Celsius.</summary>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Temperature in degrees Fahrenheit, derived from <see cref="TemperatureC"/>.
        /// The conversion truncates, so it can be off by up to one degree.
        /// </summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>Short description of the weather, for example "Warm".</summary>
        public string? Summary { get; set; }
    }
}
