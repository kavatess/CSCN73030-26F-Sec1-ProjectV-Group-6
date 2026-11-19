/**
 * @file PayrollController.cs
 * @brief Compensation and payroll headcount endpoints (the only endpoints that return salary).
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoints for pay. Each action calls one <see cref="PayrollService"/> method.</summary>
[Authorize(Policy = StaffHrPolicies.Payroll)]
[ApiController]
[Produces("application/json")]
[Tags("Payroll")]
public class PayrollController : ControllerBase
{
    private readonly PayrollService _payrollService;

    /// <summary>Creates the controller.</summary>
    /// <param name="payrollService">Payroll business logic.</param>
    public PayrollController(PayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    /// <summary>Set an employee's pay from a date. The previous rate is closed on that date. Returns the pay history.</summary>
    /// <param name="staffNumber">Employee.</param>
    /// <param name="request">Pay band, salary and start date.</param>
    [HttpPut("staff/{staffNumber}/compensation")]
    [ProducesResponseType<List<CompensationDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "The request body could not be read.",
        ExampleErrors = ["$.annualSalary=The JSON value could not be converted to System.Nullable`1[System.Decimal]."])]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["effectiveFrom=A new rate must start on or after the current rate's start (2025-01-01)."])]
    public async Task<ActionResult<List<CompensationDto>>> Set(string staffNumber, SetCompensationRequest request) =>
        await _payrollService.SetAsync(staffNumber, request);

    /// <summary>An employee's pay history, newest first.</summary>
    /// <param name="staffNumber">Employee.</param>
    [HttpGet("staff/{staffNumber}/compensation")]
    [ProducesResponseType<List<CompensationDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000999' was not found.")]
    public async Task<ActionResult<List<CompensationDto>>> History(string staffNumber) =>
        await _payrollService.ListAsync(staffNumber);

    /// <summary>Payroll headcount and salary totals per department, for the Budget service.</summary>
    /// <param name="asOf">Payroll date. Defaults to today.</param>
    [HttpGet("headcounts/payroll")]
    [ProducesResponseType<PayrollHeadcountDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status400BadRequest, "One or more query values could not be read.",
        ExampleErrors = ["asOf=The value 'today' is not valid for asOf."])]
    public async Task<ActionResult<PayrollHeadcountDto>> Headcount(DateOnly? asOf) =>
        await _payrollService.HeadcountAsync(asOf);
}
