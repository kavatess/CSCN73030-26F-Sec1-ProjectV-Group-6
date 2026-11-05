using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Roles staff hold in services over time.</summary>
public class AssignmentService(
    AssignmentRepository assignmentRepository,
    StaffRepository staffRepository,
    DepartmentRepository departmentRepository,
    Clock clock)
{
    public async Task<AssignmentDto> CreateAsync(string staffNumber, CreateAssignmentRequest request)
    {
        var staff = await staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);

        var errors = new ValidationErrors();
        errors.Required("serviceId", request.ServiceId);
        errors.Required("roleTitle", request.RoleTitle);
        errors.AddIf(staff.EmploymentStatus == EmploymentStatus.Terminated, "staffNumber",
            "A terminated employee cannot be given a new assignment.");

        var startDate = request.StartDate ?? clock.Today;
        errors.AddIf(request.EndDate != null && request.EndDate <= startDate, "endDate", "EndDate must be after StartDate.");

        Service? service = null;
        if (request.ServiceId != null)
        {
            service = await departmentRepository.GetServiceAsync(request.ServiceId.Value);
            errors.AddIf(service == null, "serviceId", $"Unknown service id {request.ServiceId}.");
        }
        errors.ThrowIfAny();

        var assignment = new StaffAssignment
        {
            StaffId = staff.StaffId,
            ServiceId = service!.ServiceId,
            RoleTitle = request.RoleTitle!.Trim(),
            StartDate = startDate,
            EndDate = request.EndDate,
        };
        assignmentRepository.Add(assignment);
        await assignmentRepository.SaveChangesAsync();

        return (await assignmentRepository.GetByIdAsync(assignment.AssignmentId))!.ToDto(clock.Today);
    }

    public async Task<List<AssignmentDto>> ListForStaffAsync(string staffNumber)
    {
        var staff = await staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);
        var assignments = await assignmentRepository.ListForStaffAsync(staff.StaffId);
        var today = clock.Today;
        return assignments.Select(a => a.ToDto(today)).ToList();
    }

    /// <summary>Sets EndDate, which closes the assignment from that day on.</summary>
    public async Task<AssignmentDto> UpdateAsync(int assignmentId, UpdateAssignmentRequest request)
    {
        var assignment = await assignmentRepository.GetByIdAsync(assignmentId)
                         ?? throw new NotFoundException("Assignment", assignmentId);

        var errors = new ValidationErrors();
        errors.Required("endDate", request.EndDate);
        errors.AddIf(request.EndDate != null && request.EndDate <= assignment.StartDate,
            "endDate", $"EndDate must be after StartDate ({assignment.StartDate:yyyy-MM-dd}).");
        errors.ThrowIfAny();

        assignment.EndDate = request.EndDate;
        await assignmentRepository.SaveChangesAsync();
        return assignment.ToDto(clock.Today);
    }
}
