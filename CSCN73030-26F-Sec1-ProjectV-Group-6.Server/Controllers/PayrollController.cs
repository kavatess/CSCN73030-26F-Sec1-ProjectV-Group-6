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
    private readonly BudgetLedgerService _ledgerService;

    /// <summary>Creates the controller.</summary>
    /// <param name="payrollService">Payroll business logic.</param>
    /// <param name="ledgerService">Payroll statements sent to Budget and Treasury.</param>
    public PayrollController(PayrollService payrollService, BudgetLedgerService ledgerService)
    {
        _payrollService = payrollService;
        _ledgerService = ledgerService;
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

    /// <summary>
    /// Sends each department's salary cost for a month to Budget and Treasury as an expense
    /// (POST /api/budget/v1/expenses): annual salaries on payroll at the month's end (today for the current month)
    /// divided by 12, department totals only. Departments Budget already accepted for the month are skipped, so
    /// calling twice records nothing twice. Returns every statement of the month, with Budget's answer.
    /// </summary>
    /// <param name="request">Month to send, "yyyy-MM"; defaults to the current month.</param>
    [HttpPost("headcounts/payroll/statements")]
    [ProducesResponseType<PayrollStatementsDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["period=Period must be a month written yyyy-MM, e.g. 2026-11."])]
    [ProblemResponse(StatusCodes.Status502BadGateway, "The Budget service answered 500: Internal error.")]
    [ProblemResponse(StatusCodes.Status503ServiceUnavailable,
        "The Budget integration is not configured (set Budget:BaseUrl). Record the funding decision by hand instead.")]
    public async Task<ActionResult<PayrollStatementsDto>> SendStatements(SendPayrollStatementsRequest? request) =>
        await _ledgerService.SendStatementsAsync(request ?? new SendPayrollStatementsRequest(null));

    /// <summary>The payroll statements already sent to Budget for a month, with Budget's answers.</summary>
    /// <param name="period">Month, "yyyy-MM"; defaults to the current month.</param>
    [HttpGet("headcounts/payroll/statements")]
    [ProducesResponseType<PayrollStatementsDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status422UnprocessableEntity, "One or more fields are invalid. See 'errors' for details.",
        ExampleErrors = ["period=Period must be a month written yyyy-MM, e.g. 2026-11."])]
    public async Task<ActionResult<PayrollStatementsDto>> Statements(string? period) => await _ledgerService.ListStatementsAsync(period);
}
