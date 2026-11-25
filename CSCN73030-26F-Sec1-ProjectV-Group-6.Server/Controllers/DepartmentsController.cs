/**
 * @file DepartmentsController.cs
 * @brief The /departments endpoints: list, services, headcount.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoints for departments and services. Each action calls one <see cref="DepartmentService"/> method.</summary>
[Authorize(Policy = StaffHrPolicies.Read)]
[ApiController]
[Route("departments")]
[Produces("application/json")]
[Tags("Departments & services")]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _departmentService;
    private readonly BudgetLedgerService _ledgerService;

    /// <summary>Creates the DepartmentsController. Dependencies are supplied by dependency injection.</summary>
    /// <param name="departmentService">Department business logic.</param>
    /// <param name="ledgerService">Department budgets from Budget and Treasury.</param>
    public DepartmentsController(DepartmentService departmentService, BudgetLedgerService ledgerService)
    {
        _departmentService = departmentService;
        _ledgerService = ledgerService;
    }

    /// <summary>All departments with their service count.</summary>
    [HttpGet]
    [ProducesResponseType<List<DepartmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DepartmentDto>>> List() => await _departmentService.ListAsync();

    /// <summary>The services in a department. Use ServiceId to filter staff, rosters and headcount.</summary>
    /// <param name="code">Department code, any case.</param>
    [HttpGet("{code}/services")]
    [ProducesResponseType<List<ServiceDto>>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Department 'XYZ' was not found.")]
    public async Task<ActionResult<List<ServiceDto>>> Services(string code) =>
        await _departmentService.ListServicesAsync(code);

    /// <summary>Per department: active staff, staff on duty right now, and open vacancies.</summary>
    [HttpGet("headcount")]
    [ProducesResponseType<List<DepartmentHeadcountDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DepartmentHeadcountDto>>> Headcount() =>
        await _departmentService.HeadcountAsync();

    /// <summary>
    /// A department's budget as Budget and Treasury reports it: allocated, spent, remaining and Budget's low-budget
    /// warning. Read live from Budget's Finance API, under Budget's code for the department (e.g. PTR → TRANSIT).
    /// </summary>
    /// <param name="code">Our department code, e.g. EMS.</param>
    [HttpGet("{code}/budget")]
    [ProducesResponseType<DepartmentBudgetDto>(StatusCodes.Status200OK)]
    [ProblemResponse(StatusCodes.Status404NotFound, "Department 'XYZ' was not found.")]
    [ProblemResponse(StatusCodes.Status502BadGateway, "The Budget service could not be reached (HttpRequestException). Try again in a minute.")]
    [ProblemResponse(StatusCodes.Status503ServiceUnavailable,
        "The Budget integration is not configured (set Budget:BaseUrl). Record the funding decision by hand instead.")]
    public async Task<ActionResult<DepartmentBudgetDto>> Budget(string code) => await _ledgerService.DepartmentBudgetAsync(code);
}
