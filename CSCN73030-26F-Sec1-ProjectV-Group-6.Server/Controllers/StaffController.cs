/**
 * @file StaffController.cs
 * @brief The /staff endpoints: onboard, profile, partial update, directory search.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoints for employees. Each action calls one <see cref="StaffService"/> method.</summary>
[ApiController]
[Route("staff")]
[Produces("application/json")]
[Tags("Staff")]
public class StaffController : ControllerBase
{
    private readonly StaffService _staffService;

    /// <summary>Creates the StaffController. Dependencies are supplied by dependency injection.</summary>
    /// <param name="staffService">Staff business logic.</param>
    public StaffController(StaffService staffService)
    {
        _staffService = staffService;
    }

    /// <summary>Onboard a new employee and issue their staff number (STF-0000000).</summary>
    /// <remarks>201 Created; the Location header points at the new profile.</remarks>
    [HttpPost]
    [ProducesResponseType<StaffProfileDto>(StatusCodes.Status201Created)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.hireDate=The JSON value could not be converted to System.Nullable`1[System.DateOnly]."])]
    [ProblemResponse(StatusCodes.Status409Conflict, "The work email 'jordan.reyes@cmes.example' is already in use by another employee.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["email=Must be a valid email address.", "homeDepartmentCode=Unknown department code 'XYZ'."])]
    /// <param name="request">The onboarding form.</param>
    public async Task<ActionResult<StaffProfileDto>> Create(CreateStaffRequest request)
    {
        var profile = await _staffService.CreateAsync(request);
        return CreatedAtAction(nameof(GetByNumber), new { staffNumber = profile.StaffNumber }, profile);
    }

    /// <summary>Full employee profile including assignments and credentials. Never includes salary.</summary>
    /// <param name="staffNumber">STF-0000000.</param>
    [HttpGet("{staffNumber}")]
    [ProducesResponseType<StaffProfileDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    public async Task<ActionResult<StaffProfileDto>> GetByNumber(string staffNumber) =>
        await _staffService.GetProfileAsync(staffNumber);

    /// <summary>Partial update. Omit fields you are not changing. The staff number cannot be changed.</summary>
    [HttpPatch("{staffNumber}")]
    [ProducesResponseType<StaffProfileDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.employmentStatus=The JSON value could not be converted to EmploymentStatus."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status409Conflict, "The work email 'jordan.reyes@cmes.example' is already in use by another employee.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["staffNumber=The staff number is permanent and cannot be changed."])]
    /// <param name="staffNumber">Employee to update.</param>
    /// <param name="request">Fields to change.</param>
    public async Task<ActionResult<StaffProfileDto>> Update(string staffNumber, UpdateStaffRequest request) =>
        await _staffService.UpdateAsync(staffNumber, request);

    /// <summary>Search the staff directory. Returns a paged envelope.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<StaffSummaryDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["status=The value 'Retired' is not valid for Status."])]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["pageSize=PageSize must be between 1 and 100."])]
    /// <param name="query">Filters and paging from the query string.</param>
    public async Task<ActionResult<PagedResult<StaffSummaryDto>>> Search([FromQuery] StaffSearchQuery query) =>
        await _staffService.SearchAsync(query);
}
