/**
 * @file RequisitionsController.cs
 * @brief The /requisitions endpoints: create (idempotent), list, get, update.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoints for hire requisitions. Each action calls one <see cref="RequisitionService"/> method.</summary>
[Authorize(Policy = StaffHrPolicies.Read)]
[ApiController]
[Route("requisitions")]
[Produces("application/json")]
[Tags("Requisitions")]
public class RequisitionsController : ControllerBase
{
    private readonly RequisitionService _requisitionService;
    private readonly BudgetFundingService _fundingService;

    /// <summary>Creates the RequisitionsController. Dependencies are supplied by dependency injection.</summary>
    /// <param name="requisitionService">Requisition business logic.</param>
    /// <param name="fundingService">Funding requests through Budget and Treasury.</param>
    public RequisitionsController(RequisitionService requisitionService, BudgetFundingService fundingService)
    {
        _requisitionService = requisitionService;
        _fundingService = fundingService;
    }

    /// <summary>
    /// Raise a hire requisition. Requires an Idempotency-Key header: replaying the same key with the
    /// same body returns the original requisition (200, header Idempotent-Replayed: true) instead of creating another.
    /// </summary>
    /// <param name="idempotencyKey">A unique value per new requisition, e.g. a GUID. Max 100 characters.</param>
    /// <param name="request">The requisition.</param>
    /// <remarks>201 when created. 200 on a replay: the key was already used with the same body, so the original is returned.</remarks>
    [Authorize(Policy = StaffHrPolicies.SubmitRequisitions)]
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
        var result = await _requisitionService.CreateAsync(idempotencyKey, request);
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
    /// <param name="query">Type, status and department filters.</param>
    public async Task<ActionResult<List<RequisitionDto>>> List([FromQuery] RequisitionQuery query) =>
        await _requisitionService.ListAsync(query);

    /// <summary>One requisition by number, e.g. REQ-0000001.</summary>
    /// <param name="number">REQ-0000000.</param>
    [HttpGet("{number}")]
    [ProducesResponseType<RequisitionDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Requisition 'REQ-0000999' was not found.")]
    public async Task<ActionResult<RequisitionDto>> Get(string number) => await _requisitionService.GetAsync(number);

    /// <summary>Record a status change, budget decision and/or filled count manually.</summary>
    [Authorize(Policy = StaffHrPolicies.Manage)]
    [HttpPatch("{number}")]
    [ProducesResponseType<RequisitionDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.status=The JSON value could not be converted to RequisitionStatus."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Requisition 'REQ-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["status=Cannot move from Received to Approved. Allowed: HRReview, Rejected, Closed."])]
    /// <param name="number">Requisition to update.</param>
    /// <param name="request">New status, budget decision and/or filled count.</param>
    public async Task<ActionResult<RequisitionDto>> Update(string number, UpdateRequisitionRequest request) =>
        await _requisitionService.UpdateAsync(number, request);

    /// <summary>
    /// Sends a hire requisition in HRReview to Budget and Treasury: creates a funding request in Budget's Finance
    /// API for the annual cost of all positions and moves the requisition to AwaitingBudget (budgetDecision Pending).
    /// The cost per position is estimated from current salaries unless annualCostPerPosition is given.
    /// </summary>
    /// <param name="number">Requisition number, e.g. REQ-0000005.</param>
    /// <param name="request">Optional cost per position and urgency.</param>
    [Authorize(Policy = StaffHrPolicies.Manage)]
    [HttpPost("{number}/funding-request")]
    [ProducesResponseType<RequisitionDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Requisition 'REQ-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status409Conflict, "REQ-0000005 was already sent to Budget (funding request 6f1c…).")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["status=Only a requisition in HRReview can be sent to Budget; this one is Received."])]
    [ProblemResponse(StatusCodes.Status502BadGateway, "The Budget service answered 503: Budget is down for maintenance.")]
    [ProblemResponse(StatusCodes.Status503ServiceUnavailable,
        "The Budget integration is not configured (set Budget:BaseUrl). Record the funding decision by hand instead.")]
    public async Task<ActionResult<RequisitionDto>> RequestFunding(string number, RequestFundingRequest? request) =>
        await _fundingService.RequestAsync(number, request ?? new RequestFundingRequest(null, null));

    /// <summary>
    /// Asks Budget now for the decision on this requisition's funding request and applies it: Approved moves the
    /// requisition to Approved, Rejected to Rejected. A background check does the same every few minutes.
    /// </summary>
    /// <param name="number">Requisition number.</param>
    [Authorize(Policy = StaffHrPolicies.Manage)]
    [HttpPost("{number}/funding-request/sync")]
    [ProducesResponseType<RequisitionDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Requisition 'REQ-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["fundingRequest=REQ-0000005 has not been sent to Budget."])]
    [ProblemResponse(StatusCodes.Status502BadGateway, "The Budget service could not be reached (HttpRequestException). Try again in a minute.")]
    public async Task<ActionResult<RequisitionDto>> SyncFunding(string number) => await _fundingService.SyncAsync(number);
}
