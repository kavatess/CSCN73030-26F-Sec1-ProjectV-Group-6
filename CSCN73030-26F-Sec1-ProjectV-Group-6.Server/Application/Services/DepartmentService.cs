/**
 * @file DepartmentService.cs
 * @brief Business logic for departments, services and department headcount.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Departments, their services, and per-department headcount.</summary>
public class DepartmentService
{
    private readonly DepartmentRepository _departmentRepository;
    private readonly Clock _clock;

    /// <summary>Creates the DepartmentService. Dependencies are supplied by dependency injection.</summary>
    /// <param name="departmentRepository">Department and service data access.</param>
    /// <param name="clock">Current UTC time.</param>
    public DepartmentService(DepartmentRepository departmentRepository, Clock clock)
    {
        _departmentRepository = departmentRepository;
        _clock = clock;
    }

    /// <summary>Every department with its service count.</summary>
    public async Task<List<DepartmentDto>> ListAsync()
    {
        var departments = await _departmentRepository.ListWithServicesAsync();
        return departments.Select(d => new DepartmentDto(d.Code, d.Name, d.Services.Count)).ToList();
    }

    /// <summary>A department's services.</summary>
    /// <param name="departmentCode">Department code, any case.</param>
    /// <exception cref="Errors.NotFoundException">The department does not exist.</exception>
    public async Task<List<ServiceDto>> ListServicesAsync(string departmentCode)
    {
        var department = await _departmentRepository.GetByCodeAsync(departmentCode)
                         ?? throw new NotFoundException("Department", departmentCode);
        var services = await _departmentRepository.ListServicesAsync(department.DepartmentId);
        return services.Select(s => new ServiceDto(s.ServiceId, s.Name, s.Department.Code)).ToList();
    }

    /// <summary>Active staff, staff on duty now, and open vacancies, per department.</summary>
    public async Task<List<DepartmentHeadcountDto>> HeadcountAsync()
    {
        var departments = await _departmentRepository.ListWithServicesAsync();
        var active = await _departmentRepository.CountActiveStaffByDepartmentAsync();
        var onDuty = await _departmentRepository.CountOnDutyByDepartmentAsync(_clock.UtcNow);
        var vacancies = await _departmentRepository.CountVacanciesByDepartmentAsync();

        return departments
            .Select(d => new DepartmentHeadcountDto(
                d.Code,
                d.Name,
                active.GetValueOrDefault(d.DepartmentId),
                onDuty.GetValueOrDefault(d.DepartmentId),
                vacancies.GetValueOrDefault(d.DepartmentId)))
            .ToList();
    }
}
