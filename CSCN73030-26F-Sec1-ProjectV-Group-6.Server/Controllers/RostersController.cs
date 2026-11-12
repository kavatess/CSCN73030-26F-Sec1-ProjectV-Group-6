/**
 * @file RostersController.cs
 * @brief The /rosters endpoints: list, add, delete, headcount, on-duty.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoints for the duty roster. Each action calls one <see cref="RosterService"/> method.</summary>
[ApiController]
[Route("rosters")]
[Produces("application/json")]
[Tags("Rosters")]
public class RostersController : ControllerBase
{
    private readonly RosterService _rosterService;

    /// <summary>Creates the RostersController. Dependencies are supplied by dependency injection.</summary>
    /// <param name="rosterService">Roster business logic.</param>
    public RostersController(RosterService rosterService)
    {
        _rosterService = rosterService;
    }

    /// <summary>Shifts overlapping [from, to) (UTC). Defaults to the 7 days starting today.</summary>
    /// <param name="service">ServiceId to filter by. Omit for all services.</param>
    /// <param name="from">Window start (UTC). Defaults to midnight today.</param>
    /// <param name="to">Window end (UTC). Defaults to from + 7 days. Max window 62 days.</param>
    [HttpGet]
    [ProducesResponseType<List<RosterEntryDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["from=The value 'tomorrow' is not valid for from."])]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["to='to' must be after 'from'."])]
    public async Task<ActionResult<List<RosterEntryDto>>> List(int? service, DateTime? from, DateTime? to) =>
        await _rosterService.ListAsync(service, from, to);

    /// <summary>Add a shift. Rejects ShiftEnd ≤ ShiftStart (422) and overlaps with the person's other shifts (409).</summary>
    [HttpPost("entries")]
    [ProducesResponseType<RosterEntryDto>(StatusCodes.Status201Created)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.shiftType=The JSON value could not be converted to ShiftType."])]
    [ProblemResponse(StatusCodes.Status409Conflict,
        "STF-0000003 is already rostered from 2026-11-05 07:00:00Z to 2026-11-05 19:00:00Z (roster entry 12), which overlaps this shift.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["shiftEnd=ShiftEnd must be after ShiftStart."])]
    /// <param name="request">The shift.</param>
    public async Task<ActionResult<RosterEntryDto>> CreateEntry(CreateRosterEntryRequest request)
    {
        var entry = await _rosterService.CreateEntryAsync(request);
        return Created($"/rosters/entries/{entry.RosterEntryId}", entry);
    }

    /// <summary>Remove a shift from the roster.</summary>
    /// <param name="id">Roster entry id.</param>
    [HttpDelete("entries/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Roster entry '42' was not found.")]
    public async Task<IActionResult> DeleteEntry(int id)
    {
        await _rosterService.DeleteEntryAsync(id);
        return NoContent();
    }

    /// <summary>Per service: staff with a current assignment (Active/Probation) and staff on duty now.</summary>
    /// <param name="service">ServiceId. Omit for every service.</param>
    [HttpGet("headcount")]
    [ProducesResponseType<List<ServiceHeadcountDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["service=Unknown service id 99."])]
    public async Task<ActionResult<List<ServiceHeadcountDto>>> Headcount(int? service) =>
        await _rosterService.HeadcountAsync(service);

    /// <summary>Shifts in progress at a given instant (UTC). Defaults to now.</summary>
    /// <param name="service">ServiceId. Omit for every service.</param>
    /// <param name="at">Instant to check (UTC). Defaults to now.</param>
    [HttpGet("on-duty")]
    [ProducesResponseType<List<RosterEntryDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["service=Unknown service id 99."])]
    public async Task<ActionResult<List<RosterEntryDto>>> OnDuty(int? service, DateTime? at) =>
        await _rosterService.OnDutyAsync(service, at);
}
