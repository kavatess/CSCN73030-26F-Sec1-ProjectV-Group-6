/**
 * @file AuditController.cs
 * @brief GET /audit: who changed what and when.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoint for the audit trail. Calls <see cref="AuditService"/>.</summary>
[ApiController]
[Route("audit")]
[Produces("application/json")]
[Tags("Audit")]
public class AuditController : ControllerBase
{
    private readonly AuditService _auditService;

    /// <summary>Creates the controller.</summary>
    /// <param name="auditService">Audit business logic.</param>
    public AuditController(AuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>Who created, updated or deleted which record, newest first. Returns a paged envelope.</summary>
    /// <param name="query">Entity type, entity id, actor and paging.</param>
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["page=The value 'first' is not valid for Page."])]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["pageSize=PageSize must be between 1 and 100."])]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Search([FromQuery] AuditQuery query) =>
        await _auditService.SearchAsync(query);
}
