/**
 * @file CredentialsController.cs
 * @brief Credential endpoints under /credentials and /staff/{staffNumber}/credentials.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoints for credentials. Each action calls one <see cref="CredentialService"/> method.</summary>
[Authorize(Policy = StaffHrPolicies.Read)]
[ApiController]
[Produces("application/json")]
[Tags("Credentials")]
public class CredentialsController : ControllerBase
{
    private readonly CredentialService _credentialService;

    /// <summary>Creates the CredentialsController. Dependencies are supplied by dependency injection.</summary>
    /// <param name="credentialService">Credential business logic.</param>
    public CredentialsController(CredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    /// <summary>Search credentials across the city, soonest expiry first. Returns a paged envelope.</summary>
    [HttpGet("credentials")]
    [ProducesResponseType<PagedResult<CredentialDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["state=The value 'Lapsed' is not valid for State."])]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["page=Page must be 1 or more."])]
    /// <param name="query">State, type, department and paging.</param>
    public async Task<ActionResult<PagedResult<CredentialDto>>> Search([FromQuery] CredentialQuery query) =>
        await _credentialService.SearchAsync(query);

    /// <summary>Every kind of credential. Use credentialTypeId when recording a credential or filtering by type.</summary>
    [HttpGet("credentials/types")]
    [ProducesResponseType<List<CredentialTypeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CredentialTypeDto>>> Types() =>
        await _credentialService.ListTypesAsync();

    /// <summary>Credentials that have not expired yet but will within the given number of days. Suspended ones are excluded.</summary>
    /// <param name="withinDays">0 to 365. Defaults to 30.</param>
    [HttpGet("credentials/expiring")]
    [ProducesResponseType<List<CredentialDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["withinDays=withinDays must be between 0 and 365."])]
    public async Task<ActionResult<List<CredentialDto>>> Expiring(int withinDays = 30) =>
        await _credentialService.ExpiringAsync(withinDays);

    /// <summary>Record a new credential for a staff member.</summary>
    [Authorize(Policy = StaffHrPolicies.Manage)]
    [HttpPost("staff/{staffNumber}/credentials")]
    [ProducesResponseType<CredentialDto>(StatusCodes.Status201Created)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.expiryDate=The JSON value could not be converted to System.Nullable`1[System.DateOnly]."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["expiryDate=ExpiryDate must be after IssueDate."])]
    /// <param name="staffNumber">Holder.</param>
    /// <param name="request">Type, number and dates.</param>
    public async Task<ActionResult<CredentialDto>> Create(string staffNumber, CreateCredentialRequest request)
    {
        var credential = await _credentialService.CreateAsync(staffNumber, request);
        return Created($"/credentials/{credential.CredentialId}", credential);
    }

    /// <summary>Record a renewal (new number / dates) or suspend / lift a suspension.</summary>
    [Authorize(Policy = StaffHrPolicies.Manage)]
    [HttpPatch("credentials/{id:int}")]
    [ProducesResponseType<CredentialDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.suspended=The JSON value could not be converted to System.Nullable`1[System.Boolean]."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Credential '42' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["expiryDate=ExpiryDate must be after IssueDate."])]
    /// <param name="id">Credential id.</param>
    /// <param name="request">New number, dates and/or suspension.</param>
    public async Task<ActionResult<CredentialDto>> Update(int id, UpdateCredentialRequest request) =>
        await _credentialService.UpdateAsync(id, request);
}
