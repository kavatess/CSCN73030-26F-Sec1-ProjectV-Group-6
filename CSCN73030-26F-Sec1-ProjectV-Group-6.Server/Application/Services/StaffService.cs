/**
 * @file StaffService.cs
 * @brief Business logic for creating, reading, updating and searching staff.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Events;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Create, read, update and search employees.</summary>
public class StaffService
{
    private readonly StaffRepository _staffRepository;
    private readonly DepartmentRepository _departmentRepository;
    private readonly Clock _clock;
    private readonly OutboxWriter _outbox;

    /// <summary>Creates the StaffService. Dependencies are supplied by dependency injection.</summary>
    /// <param name="staffRepository">Staff data access.</param>
    /// <param name="departmentRepository">Department and service data access.</param>
    /// <param name="clock">Current UTC time.</param>
    /// <param name="outbox">Queues integration events in the same transaction.</param>
    public StaffService(StaffRepository staffRepository, DepartmentRepository departmentRepository, Clock clock, OutboxWriter outbox)
    {
        _outbox = outbox;
        _staffRepository = staffRepository;
        _departmentRepository = departmentRepository;
        _clock = clock;
    }

    private const int MinimumAgeAtHire = 16;
    /// <summary>Largest page size a search accepts.</summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Creates an employee and issues a staff number. The number is taken from the sequence and the
    /// Staff row (plus optional first assignment) is inserted inside one transaction (business rule 1).
    /// </summary>
    /// <param name="request">The onboarding form.</param>
    /// <returns>The new employee's profile.</returns>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    /// <exception cref="Errors.ConflictException">The work email is already in use (409).</exception>
    /// @par Business rules 1 and 2
    /// The staff number comes from the sequence inside the same transaction as the insert; a duplicate email is 409.
    public async Task<StaffProfileDto> CreateAsync(CreateStaffRequest request)
    {
        var errors = new ValidationErrors();
        errors.Required("firstName", request.FirstName);
        errors.Required("lastName", request.LastName);
        errors.MaxLength("preferredName", request.PreferredName, 100);
        errors.Required("email", request.Email, 254);
        errors.Email("email", request.Email);
        errors.MaxLength("phone", request.Phone, 30);
        errors.MaxLength("citizenId", request.CitizenId, 30);
        errors.Required("dateOfBirth", request.DateOfBirth);
        errors.Required("hireDate", request.HireDate);
        errors.Required("employmentType", request.EmploymentType);
        errors.Required("homeDepartmentCode", request.HomeDepartmentCode, 10);
        errors.AddIf(request.EmploymentStatus is not (null or EmploymentStatus.Active or EmploymentStatus.Probation),
            "employmentStatus", "A new employee must start as Active or Probation.");
        ValidateBirthAndHireDates(errors, request.DateOfBirth, request.HireDate);

        var department = string.IsNullOrWhiteSpace(request.HomeDepartmentCode)
            ? null
            : await _departmentRepository.GetByCodeAsync(request.HomeDepartmentCode);
        errors.AddIf(department == null && !string.IsNullOrWhiteSpace(request.HomeDepartmentCode),
            "homeDepartmentCode", $"Unknown department code '{request.HomeDepartmentCode}'.");

        Service? service = null;
        var first = request.InitialAssignment;
        if (first != null)
        {
            errors.Required("initialAssignment.serviceId", first.ServiceId);
            errors.Required("initialAssignment.roleTitle", first.RoleTitle);
            if (first.ServiceId != null)
            {
                service = await _departmentRepository.GetServiceAsync(first.ServiceId.Value);
                errors.AddIf(service == null, "initialAssignment.serviceId", $"Unknown service id {first.ServiceId}.");
            }
            var start = first.StartDate ?? request.HireDate ?? _clock.Today;
            errors.AddIf(first.EndDate != null && first.EndDate <= start,
                "initialAssignment.endDate", "EndDate must be after StartDate.");
        }

        errors.ThrowIfAny();

        var email = Text.NormaliseEmail(request.Email!);
        if (await _staffRepository.EmailInUseAsync(email))
            throw new ConflictException($"The work email '{email}' is already in use by another employee.");

        await using var transaction = await _staffRepository.BeginTransactionAsync();

        var staff = new Staff
        {
            StaffNumber = await _staffRepository.NextStaffNumberAsync(),
            FirstName = request.FirstName!.Trim(),
            LastName = request.LastName!.Trim(),
            PreferredName = Text.TrimToNull(request.PreferredName),
            Email = email,
            DateOfBirth = request.DateOfBirth!.Value,
            Phone = Text.TrimToNull(request.Phone),
            CitizenId = Text.TrimToNull(request.CitizenId),
            HireDate = request.HireDate!.Value,
            EmploymentType = request.EmploymentType!.Value,
            EmploymentStatus = request.EmploymentStatus ?? EmploymentStatus.Probation,
            HomeDepartmentId = department!.DepartmentId,
        };

        if (first != null)
        {
            staff.Assignments.Add(new StaffAssignment
            {
                ServiceId = service!.ServiceId,
                RoleTitle = first.RoleTitle!.Trim(),
                StartDate = first.StartDate ?? staff.HireDate,
                EndDate = first.EndDate,
            });
        }

        _staffRepository.Add(staff);
        // Committed with the Staff row, so the event exists if and only if the hire does.
        _outbox.Enqueue(IntegrationEventTypes.StaffOnboarded, new StaffOnboardedEvent(
            staff.StaffNumber, staff.FirstName, staff.LastName, staff.DisplayName, staff.Email,
            department.Code, staff.EmploymentType.ToString(), staff.EmploymentStatus.ToString(), staff.HireDate,
            service?.ServiceId, staff.Assignments.FirstOrDefault()?.RoleTitle));
        await _staffRepository.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetProfileAsync(staff.StaffNumber);
    }

    /// <summary>Loads one employee's full profile.</summary>
    /// <param name="staffNumber">STF-0000000.</param>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    public async Task<StaffProfileDto> GetProfileAsync(string staffNumber)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber, withDetails: true)
                    ?? throw new NotFoundException("Staff", staffNumber);
        return staff.ToProfile(_clock.Today);
    }

    /// <summary>Partial update. Null fields are left unchanged; the staff number can never change.</summary>
    /// <param name="staffNumber">Employee to update.</param>
    /// <param name="request">Fields to change.</param>
    /// <returns>The updated profile.</returns>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    /// <exception cref="Errors.ConflictException">The new email is already in use (409).</exception>
    /// @par Business rules 1 and 2
    /// A different staffNumber in the body is 422; an email already in use is 409.
    public async Task<StaffProfileDto> UpdateAsync(string staffNumber, UpdateStaffRequest request)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);

        var errors = new ValidationErrors();
        // Business rule 1: reject any attempt to change the staff number.
        errors.AddIf(request.StaffNumber != null && request.StaffNumber.Trim() != staff.StaffNumber,
            "staffNumber", "The staff number is permanent and cannot be changed.");
        errors.NotBlankIfPresent("firstName", request.FirstName);
        errors.NotBlankIfPresent("lastName", request.LastName);
        errors.MaxLength("preferredName", request.PreferredName, 100);
        errors.NotBlankIfPresent("email", request.Email, 254);
        errors.Email("email", request.Email);
        errors.MaxLength("phone", request.Phone, 30);
        errors.MaxLength("citizenId", request.CitizenId, 30);
        errors.AddIf(request.EmploymentStatus == EmploymentStatus.Terminated, "employmentStatus",
            "Termination must go through a termination requisition, which is not available in this release.");
        ValidateBirthAndHireDates(errors, request.DateOfBirth ?? staff.DateOfBirth, request.HireDate ?? staff.HireDate);

        Department? newDepartment = null;
        if (request.HomeDepartmentCode != null)
        {
            newDepartment = await _departmentRepository.GetByCodeAsync(request.HomeDepartmentCode);
            errors.AddIf(newDepartment == null, "homeDepartmentCode", $"Unknown department code '{request.HomeDepartmentCode}'.");
        }

        errors.ThrowIfAny();

        if (request.Email != null)
        {
            var email = Text.NormaliseEmail(request.Email);
            if (email != staff.Email && await _staffRepository.EmailInUseAsync(email, staff.StaffId))
                throw new ConflictException($"The work email '{email}' is already in use by another employee.");
            staff.Email = email;
        }

        if (request.FirstName != null) staff.FirstName = request.FirstName.Trim();
        if (request.LastName != null) staff.LastName = request.LastName.Trim();
        if (request.PreferredName != null) staff.PreferredName = Text.TrimToNull(request.PreferredName);
        if (request.Phone != null) staff.Phone = Text.TrimToNull(request.Phone);
        if (request.CitizenId != null) staff.CitizenId = Text.TrimToNull(request.CitizenId);
        if (request.DateOfBirth != null) staff.DateOfBirth = request.DateOfBirth.Value;
        if (request.HireDate != null) staff.HireDate = request.HireDate.Value;
        if (request.EmploymentType != null) staff.EmploymentType = request.EmploymentType.Value;
        if (request.EmploymentStatus != null) staff.EmploymentStatus = request.EmploymentStatus.Value;
        // A transfer changes the home department only; the staff number stays the same.
        if (newDepartment != null) staff.HomeDepartmentId = newDepartment.DepartmentId;

        await _staffRepository.SaveChangesAsync();
        return await GetProfileAsync(staff.StaffNumber);
    }

    /// <summary>Searches the staff directory.</summary>
    /// <param name="query">Filters and paging.</param>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<PagedResult<StaffSummaryDto>> SearchAsync(StaffSearchQuery query)
    {
        var errors = new ValidationErrors();
        ValidatePaging(errors, query.Page, query.PageSize);

        int? departmentId = null;
        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            var department = await _departmentRepository.GetByCodeAsync(query.Department);
            errors.AddIf(department == null, "department", $"Unknown department code '{query.Department}'.");
            departmentId = department?.DepartmentId;
        }
        errors.ThrowIfAny();

        var today = _clock.Today;
        var (items, total) = await _staffRepository.SearchAsync(
            new StaffSearchCriteria(query.Q, departmentId, query.Service, query.Role, query.Status,
                query.CredentialState, query.Page, query.PageSize),
            today);

        return new PagedResult<StaffSummaryDto>(
            items.Select(s => s.ToSummary(today)).ToList(), query.Page, query.PageSize, total);
    }

    /// <summary>Checks page is at least 1 and pageSize is 1 to <see cref="MaxPageSize"/>.</summary>
    /// <param name="errors">Collector to add errors to.</param>
    /// <param name="page">Requested page.</param>
    /// <param name="pageSize">Requested page size.</param>
    public static void ValidatePaging(ValidationErrors errors, int page, int pageSize)
    {
        errors.AddIf(page < 1, "page", "Page must be 1 or more.");
        errors.AddIf(pageSize is < 1 or > MaxPageSize, "pageSize", $"PageSize must be between 1 and {MaxPageSize}.");
    }

    private void ValidateBirthAndHireDates(ValidationErrors errors, DateOnly? dateOfBirth, DateOnly? hireDate)
    {
        if (dateOfBirth != null)
            errors.AddIf(dateOfBirth >= _clock.Today, "dateOfBirth", "Date of birth must be in the past.");
        if (dateOfBirth != null && hireDate != null)
            errors.AddIf(hireDate < dateOfBirth.Value.AddYears(MinimumAgeAtHire), "hireDate",
                $"Hire date must be at least {MinimumAgeAtHire} years after date of birth.");
    }
}
