/**
 * @file HealthController.cs
 * @brief GET /health endpoint.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>Reports whether the service and its database are up.</summary>
[AllowAnonymous]
[ApiController]
[Route("health")]
[Produces("application/json")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    /// <summary>Creates the HealthController. Dependencies are supplied by dependency injection.</summary>
    /// <param name="healthCheckService">Runs the registered health checks.</param>
    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>Liveness and database connectivity.</summary>
    /// <remarks>200 when the service and its database are reachable; 503 when the database is unreachable.</remarks>
    [HttpGet]
    [ProducesResponseType<HealthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthDto>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthDto>> Get(CancellationToken ct)
    {
        var report = await _healthCheckService.CheckHealthAsync(ct);
        var body = new HealthDto(
            report.Status.ToString(),
            report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()));

        return report.Status == HealthStatus.Healthy
            ? Ok(body)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
