/**
 * @file SecurityFlagsController.cs
 * @brief Security flag sync endpoints for the Security service.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoints for security flags. Each action calls one <see cref="SecurityFlagService"/> method.</summary>
[Authorize(Policy = StaffHrPolicies.SecurityFlags)]
[ApiController]
[Produces("application/json")]
[Tags("Security flags")]
public class SecurityFlagsController : ControllerBase
{
    private readonly SecurityFlagService _flagService;

    /// <summary>Creates the controller.</summary>
    /// <param name="flagService">Security flag business logic.</param>
    public SecurityFlagsController(SecurityFlagService flagService)
    {
        _flagService = flagService;
    }

    /// <summary>
    /// Sync one flag from the Security service: creates it (201) or updates it (200). An update older
    /// than the stored one is ignored (200, header Sync-Result: ignored-stale). An Active flag makes the
    /// person ineligible for duty.
    /// </summary>
    /// <param name="staffNumber">Flagged staff member.</param>
    /// <param name="flagType">Kind of flag, e.g. UnderInvestigation.</param>
    /// <param name="request">Status and when it changed.</param>
    [HttpPut("staff/{staffNumber}/security-flags/{flagType}")]
    [ProducesResponseType<SecurityFlagDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<SecurityFlagDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.status=The JSON value could not be converted to SecurityFlagStatus."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["flagType=Flag type must be 1-50 letters, digits, '.', '-' or '_'."])]
    public async Task<ActionResult<SecurityFlagDto>> Upsert(string staffNumber, string flagType, UpsertSecurityFlagRequest request)
    {
        var result = await _flagService.UpsertAsync(staffNumber, flagType, request);
        Response.Headers["Sync-Result"] = result.Outcome switch
        {
            SecurityFlagService.UpsertOutcome.Created => "created",
            SecurityFlagService.UpsertOutcome.Updated => "updated",
            _ => "ignored-stale",
        };
        return result.Outcome == SecurityFlagService.UpsertOutcome.Created
            ? Created($"/staff/{result.Flag.StaffNumber}/security-flags/{result.Flag.FlagType}", result.Flag)
            : Ok(result.Flag);
    }

    /// <summary>Every security flag of a staff member, by type.</summary>
    /// <param name="staffNumber">Staff member.</param>
    [HttpGet("staff/{staffNumber}/security-flags")]
    [ProducesResponseType<List<SecurityFlagDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    public async Task<ActionResult<List<SecurityFlagDto>>> ListForStaff(string staffNumber) =>
        await _flagService.ListForStaffAsync(staffNumber);

    /// <summary>Security flags across the city, most recently changed first, e.g. every Active flag for reconciliation.</summary>
    /// <param name="query">Optional status and flag type.</param>
    [HttpGet("security-flags")]
    [ProducesResponseType<List<SecurityFlagDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["status=The value 'Open' is not valid for Status."])]
    public async Task<ActionResult<List<SecurityFlagDto>>> Search([FromQuery] SecurityFlagQuery query) =>
        await _flagService.SearchAsync(query);
}
