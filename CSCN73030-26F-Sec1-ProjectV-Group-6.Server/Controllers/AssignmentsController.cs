using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

[ApiController]
[Produces("application/json")]
[Tags("Assignments")]
public class AssignmentsController(AssignmentService assignmentService) : ControllerBase
{
    /// <summary>Give a staff member a role in a service.</summary>
    [HttpPost("staff/{staffNumber}/assignments")]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status201Created)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.startDate=The JSON value could not be converted to System.Nullable`1[System.DateOnly]."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["endDate=EndDate must be after StartDate."])]
    public async Task<ActionResult<AssignmentDto>> Create(string staffNumber, CreateAssignmentRequest request)
    {
        var assignment = await assignmentService.CreateAsync(staffNumber, request);
        return CreatedAtAction(nameof(ListForStaff), new { staffNumber }, assignment);
    }

    /// <summary>All assignments (current and past) for a staff member, newest first.</summary>
    [HttpGet("staff/{staffNumber}/assignments")]
    [ProducesResponseType<List<AssignmentDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    public async Task<ActionResult<List<AssignmentDto>>> ListForStaff(string staffNumber) =>
        await assignmentService.ListForStaffAsync(staffNumber);

    /// <summary>Close an assignment by setting its EndDate.</summary>
    [HttpPatch("assignments/{id:int}")]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.endDate=The JSON value could not be converted to System.Nullable`1[System.DateOnly]."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Assignment '42' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["endDate=EndDate must be after StartDate (2026-03-01)."])]
    public async Task<ActionResult<AssignmentDto>> Update(int id, UpdateAssignmentRequest request) =>
        await assignmentService.UpdateAsync(id, request);
}
