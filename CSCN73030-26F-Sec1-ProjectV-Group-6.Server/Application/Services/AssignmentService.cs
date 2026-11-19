/**
 * @file AssignmentService.cs
 * @brief Business logic for staff assignments.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Roles staff hold in services over time.</summary>
public class AssignmentService
{
    private readonly AssignmentRepository _assignmentRepository;
    private readonly StaffRepository _staffRepository;
    private readonly DepartmentRepository _departmentRepository;
    private readonly Clock _clock;

    /// <summary>Creates the AssignmentService. Dependencies are supplied by dependency injection.</summary>
    /// <param name="assignmentRepository">Assignment data access.</param>
    /// <param name="staffRepository">Staff data access.</param>
    /// <param name="departmentRepository">Department and service data access.</param>
    /// <param name="clock">Current UTC time.</param>
    public AssignmentService(AssignmentRepository assignmentRepository, StaffRepository staffRepository, DepartmentRepository departmentRepository, Clock clock)
    {
        _assignmentRepository = assignmentRepository;
        _staffRepository = staffRepository;
        _departmentRepository = departmentRepository;
        _clock = clock;
    }

    /// <summary>Gives a staff member a role in a service. StartDate defaults to today; EndDate must be after it (rule 6).</summary>
    /// <param name="staffNumber">Staff member.</param>
    /// <param name="request">Service, role and dates.</param>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<AssignmentDto> CreateAsync(string staffNumber, CreateAssignmentRequest request)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);

        var errors = new ValidationErrors();
        errors.Required("serviceId", request.ServiceId);
        errors.Required("roleTitle", request.RoleTitle);
        errors.AddIf(staff.EmploymentStatus == EmploymentStatus.Terminated, "staffNumber",
            "A terminated employee cannot be given a new assignment.");

        var startDate = request.StartDate ?? _clock.Today;
        errors.AddIf(request.EndDate != null && request.EndDate <= startDate, "endDate", "EndDate must be after StartDate.");

        Service? service = null;
        if (request.ServiceId != null)
        {
            service = await _departmentRepository.GetServiceAsync(request.ServiceId.Value);
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
        _assignmentRepository.Add(assignment);
        await _assignmentRepository.SaveChangesAsync();

        return (await _assignmentRepository.GetByIdAsync(assignment.AssignmentId))!.ToDto(_clock.Today);
    }

    /// <summary>Every assignment of a staff member, newest first.</summary>
    /// <param name="staffNumber">Staff member.</param>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    public async Task<List<AssignmentDto>> ListForStaffAsync(string staffNumber)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);
        var assignments = await _assignmentRepository.ListForStaffAsync(staff.StaffId);
        var today = _clock.Today;
        return assignments.Select(a => a.ToDto(today)).ToList();
    }

    /// <summary>Sets EndDate, which closes the assignment from that day on.</summary>
    /// <param name="assignmentId">Assignment to close.</param>
    /// <param name="request">The new EndDate.</param>
    /// <exception cref="Errors.NotFoundException">The assignment does not exist.</exception>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<AssignmentDto> UpdateAsync(int assignmentId, UpdateAssignmentRequest request)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId)
                         ?? throw new NotFoundException("Assignment", assignmentId);

        var errors = new ValidationErrors();
        errors.Required("endDate", request.EndDate);
        errors.AddIf(request.EndDate != null && request.EndDate <= assignment.StartDate,
            "endDate", $"EndDate must be after StartDate ({assignment.StartDate:yyyy-MM-dd}).");
        errors.ThrowIfAny();

        assignment.EndDate = request.EndDate;
        await _assignmentRepository.SaveChangesAsync();
        return assignment.ToDto(_clock.Today);
    }
}
