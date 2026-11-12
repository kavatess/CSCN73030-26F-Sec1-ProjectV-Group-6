/**
 * @file DepartmentsController.cs
 * @brief The /departments endpoints: list, services, headcount.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Controllers;

/// <summary>HTTP endpoints for departments and services. Each action calls one <see cref="DepartmentService"/> method.</summary>
[ApiController]
[Route("departments")]
[Produces("application/json")]
[Tags("Departments & services")]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _departmentService;

    /// <summary>Creates the DepartmentsController. Dependencies are supplied by dependency injection.</summary>
    /// <param name="departmentService">Department business logic.</param>
    public DepartmentsController(DepartmentService departmentService)
    {
        _departmentService = departmentService;
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
}
