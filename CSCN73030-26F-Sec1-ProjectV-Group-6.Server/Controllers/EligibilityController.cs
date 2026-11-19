/**
 * @file EligibilityController.cs
 * @brief GET /staff/{staffNumber}/eligibility for Emergency Services, Security and Transport.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoint for duty eligibility. Calls <see cref="EligibilityService"/>.</summary>
[Authorize(Policy = StaffHrPolicies.Read)]
[ApiController]
[Produces("application/json")]
[Tags("Eligibility")]
public class EligibilityController : ControllerBase
{
    private readonly EligibilityService _eligibilityService;

    /// <summary>Creates the controller.</summary>
    /// <param name="eligibilityService">Eligibility business logic.</param>
    public EligibilityController(EligibilityService eligibilityService)
    {
        _eligibilityService = eligibilityService;
    }

    /// <summary>
    /// Whether a staff member may work a shift: Active or Probation status, a current assignment (in the
    /// service, if given), no expired or suspended duty-blocking credential and no Active security flag.
    /// </summary>
    /// <param name="staffNumber">Staff member.</param>
    /// <param name="service">ServiceId the shift is in. Omit to accept any current assignment.</param>
    /// <param name="at">Instant to check (UTC). Defaults to now.</param>
    [HttpGet("staff/{staffNumber}/eligibility")]
    [ProducesResponseType<DutyEligibilityDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["at=The value 'tomorrow' is not valid for at."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["service=Unknown service id 99."])]
    public async Task<ActionResult<DutyEligibilityDto>> Get(string staffNumber, int? service, DateTime? at) =>
        await _eligibilityService.CheckAsync(staffNumber, service, at);
}
