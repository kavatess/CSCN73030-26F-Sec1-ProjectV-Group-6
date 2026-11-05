using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Departments, their services, and per-department headcount.</summary>
public class DepartmentService(DepartmentRepository departmentRepository, Clock clock)
{
    public async Task<List<DepartmentDto>> ListAsync()
    {
        var departments = await departmentRepository.ListWithServicesAsync();
        return departments.Select(d => new DepartmentDto(d.Code, d.Name, d.Services.Count)).ToList();
    }

    public async Task<List<ServiceDto>> ListServicesAsync(string departmentCode)
    {
        var department = await departmentRepository.GetByCodeAsync(departmentCode)
                         ?? throw new NotFoundException("Department", departmentCode);
        var services = await departmentRepository.ListServicesAsync(department.DepartmentId);
        return services.Select(s => new ServiceDto(s.ServiceId, s.Name, s.Department.Code)).ToList();
    }

    public async Task<List<DepartmentHeadcountDto>> HeadcountAsync()
    {
        var departments = await departmentRepository.ListWithServicesAsync();
        var active = await departmentRepository.CountActiveStaffByDepartmentAsync();
        var onDuty = await departmentRepository.CountOnDutyByDepartmentAsync(clock.UtcNow);
        var vacancies = await departmentRepository.CountVacanciesByDepartmentAsync();

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
