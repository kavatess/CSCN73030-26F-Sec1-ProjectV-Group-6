using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

[ApiController]
[Route("requisitions")]
[Produces("application/json")]
[Tags("Requisitions")]
public class RequisitionsController(RequisitionService requisitionService) : ControllerBase
{
    /// <summary>
    /// Raise a hire requisition. Requires an Idempotency-Key header: replaying the same key with the
    /// same body returns the original requisition (200, header Idempotent-Replayed: true) instead of creating another.
    /// </summary>
    /// <param name="idempotencyKey">A unique value per new requisition, e.g. a GUID. Max 100 characters.</param>
    /// <param name="request">The requisition.</param>
    /// <response code="201">Created.</response>
    /// <response code="200">Replay: this Idempotency-Key was already used with the same body; the original is returned.</response>
    [HttpPost]
    [ProducesResponseType<RequisitionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RequisitionDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest,
        "The Idempotency-Key header is required. Send a unique value (e.g. a GUID) per new requisition.")]
    [ProblemResponse(StatusCodes.Status409Conflict,
        "Idempotency-Key 'b7e0c1d2-…' was already used for requisition REQ-0000004 with a different request body. Use a new key for a new requisition.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["type=Only Hire requisitions are supported in this release.", "positions=Positions must be between 1 and 100."])]
    public async Task<ActionResult<RequisitionDto>> Create(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CreateRequisitionRequest request)
    {
        var result = await requisitionService.CreateAsync(idempotencyKey, request);
        if (result.Replayed)
        {
            Response.Headers["Idempotent-Replayed"] = "true";
            return Ok(result.Requisition);
        }
        return CreatedAtAction(nameof(Get), new { number = result.Requisition.RequisitionNumber }, result.Requisition);
    }

    /// <summary>List requisitions, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<List<RequisitionDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["status=The value 'Pending' is not valid for Status."])]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["department=Unknown department code 'XYZ'."])]
    public async Task<ActionResult<List<RequisitionDto>>> List([FromQuery] RequisitionQuery query) =>
        await requisitionService.ListAsync(query);

    /// <summary>One requisition by number, e.g. REQ-0000001.</summary>
    [HttpGet("{number}")]
    [ProducesResponseType<RequisitionDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Requisition 'REQ-0000999' was not found.")]
    public async Task<ActionResult<RequisitionDto>> Get(string number) => await requisitionService.GetAsync(number);

    /// <summary>Record a status change, budget decision and/or filled count manually.</summary>
    [HttpPatch("{number}")]
    [ProducesResponseType<RequisitionDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.status=The JSON value could not be converted to RequisitionStatus."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Requisition 'REQ-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["status=Cannot move from Received to Approved. Allowed: HRReview, Rejected, Closed."])]
    public async Task<ActionResult<RequisitionDto>> Update(string number, UpdateRequisitionRequest request) =>
        await requisitionService.UpdateAsync(number, request);
}
