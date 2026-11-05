using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

[ApiController]
[Route("health")]
[Produces("application/json")]
[Tags("Health")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>Liveness and database connectivity.</summary>
    /// <response code="200">The service and its database are reachable.</response>
    /// <response code="503">The database is unreachable.</response>
    [HttpGet]
    [ProducesResponseType<HealthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthDto>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthDto>> Get(CancellationToken ct)
    {
        var report = await healthCheckService.CheckHealthAsync(ct);
        var body = new HealthDto(
            report.Status.ToString(),
            report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()));

        return report.Status == HealthStatus.Healthy
            ? Ok(body)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
