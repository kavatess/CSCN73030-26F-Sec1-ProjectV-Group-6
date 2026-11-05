using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

[ApiController]
[Route("staff")]
[Produces("application/json")]
[Tags("Staff")]
public class StaffController(StaffService staffService) : ControllerBase
{
    /// <summary>Onboard a new employee and issue their staff number (STF-0000000).</summary>
    /// <response code="201">Created. The Location header points at the new profile.</response>
    [HttpPost]
    [ProducesResponseType<StaffProfileDto>(StatusCodes.Status201Created)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.hireDate=The JSON value could not be converted to System.Nullable`1[System.DateOnly]."])]
    [ProblemResponse(StatusCodes.Status409Conflict, "The work email 'jordan.reyes@cmes.example' is already in use by another employee.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["email=Must be a valid email address.", "homeDepartmentCode=Unknown department code 'XYZ'."])]
    public async Task<ActionResult<StaffProfileDto>> Create(CreateStaffRequest request)
    {
        var profile = await staffService.CreateAsync(request);
        return CreatedAtAction(nameof(GetByNumber), new { staffNumber = profile.StaffNumber }, profile);
    }

    /// <summary>Full employee profile including assignments and credentials. Never includes salary.</summary>
    [HttpGet("{staffNumber}")]
    [ProducesResponseType<StaffProfileDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    public async Task<ActionResult<StaffProfileDto>> GetByNumber(string staffNumber) =>
        await staffService.GetProfileAsync(staffNumber);

    /// <summary>Partial update. Omit fields you are not changing. The staff number cannot be changed.</summary>
    [HttpPatch("{staffNumber}")]
    [ProducesResponseType<StaffProfileDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.employmentStatus=The JSON value could not be converted to EmploymentStatus."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status409Conflict, "The work email 'jordan.reyes@cmes.example' is already in use by another employee.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["staffNumber=The staff number is permanent and cannot be changed."])]
    public async Task<ActionResult<StaffProfileDto>> Update(string staffNumber, UpdateStaffRequest request) =>
        await staffService.UpdateAsync(staffNumber, request);

    /// <summary>Search the staff directory. Returns a paged envelope.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<StaffSummaryDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["status=The value 'Retired' is not valid for Status."])]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["pageSize=PageSize must be between 1 and 100."])]
    public async Task<ActionResult<PagedResult<StaffSummaryDto>>> Search([FromQuery] StaffSearchQuery query) =>
        await staffService.SearchAsync(query);
}
