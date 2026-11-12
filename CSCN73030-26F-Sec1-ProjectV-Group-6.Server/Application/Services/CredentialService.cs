/**
 * @file CredentialService.cs
 * @brief Business logic for credentials.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Licences and certifications. Status is always derived on read (business rule 5).</summary>
public class CredentialService
{
    private readonly CredentialRepository _credentialRepository;
    private readonly StaffRepository _staffRepository;
    private readonly DepartmentRepository _departmentRepository;
    private readonly Clock _clock;

    /// <summary>Creates the CredentialService. Dependencies are supplied by dependency injection.</summary>
    /// <param name="credentialRepository">Credential data access.</param>
    /// <param name="staffRepository">Staff data access.</param>
    /// <param name="departmentRepository">Department and service data access.</param>
    /// <param name="clock">Current UTC time.</param>
    public CredentialService(CredentialRepository credentialRepository, StaffRepository staffRepository, DepartmentRepository departmentRepository, Clock clock)
    {
        _credentialRepository = credentialRepository;
        _staffRepository = staffRepository;
        _departmentRepository = departmentRepository;
        _clock = clock;
    }

    /// <summary>Largest withinDays accepted by the expiring list.</summary>
    public const int MaxWithinDays = 365;

    /// <summary>Searches credentials city-wide, soonest expiry first.</summary>
    /// <param name="query">State, type, department and paging.</param>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<PagedResult<CredentialDto>> SearchAsync(CredentialQuery query)
    {
        var errors = new ValidationErrors();
        StaffService.ValidatePaging(errors, query.Page, query.PageSize);

        int? departmentId = null;
        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            var department = await _departmentRepository.GetByCodeAsync(query.Department);
            errors.AddIf(department == null, "department", $"Unknown department code '{query.Department}'.");
            departmentId = department?.DepartmentId;
        }
        errors.ThrowIfAny();

        var today = _clock.Today;
        var (items, total) = await _credentialRepository.SearchAsync(
            query.State, query.Type, departmentId, today, query.Page, query.PageSize);
        return new PagedResult<CredentialDto>(items.Select(c => c.ToDto(today)).ToList(), query.Page, query.PageSize, total);
    }

    /// <summary>Credentials (not suspended, not yet expired) whose expiry falls in the next <paramref name="withinDays"/> days.</summary>
    /// <param name="withinDays">0 to 365.</param>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<List<CredentialDto>> ExpiringAsync(int withinDays)
    {
        if (withinDays is < 0 or > MaxWithinDays)
            throw new ValidationFailedException("withinDays", $"withinDays must be between 0 and {MaxWithinDays}.");

        var today = _clock.Today;
        var credentials = await _credentialRepository.ExpiringAsync(today, today.AddDays(withinDays));
        return credentials.Select(c => c.ToDto(today)).ToList();
    }

    /// <summary>Records a new credential for a staff member. IssueDate must be before ExpiryDate (rule 6).</summary>
    /// <param name="staffNumber">Holder.</param>
    /// <param name="request">Type, number and dates.</param>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<CredentialDto> CreateAsync(string staffNumber, CreateCredentialRequest request)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);

        var errors = new ValidationErrors();
        errors.Required("credentialTypeId", request.CredentialTypeId);
        errors.Required("number", request.Number, 50);
        errors.Required("issueDate", request.IssueDate);
        errors.Required("expiryDate", request.ExpiryDate);
        // Business rule 6: IssueDate < ExpiryDate.
        errors.AddIf(request.IssueDate >= request.ExpiryDate, "expiryDate", "ExpiryDate must be after IssueDate.");

        CredentialType? type = null;
        if (request.CredentialTypeId != null)
        {
            type = await _credentialRepository.GetTypeAsync(request.CredentialTypeId.Value);
            errors.AddIf(type == null, "credentialTypeId", $"Unknown credential type id {request.CredentialTypeId}.");
        }
        errors.ThrowIfAny();

        var credential = new Credential
        {
            StaffId = staff.StaffId,
            CredentialTypeId = type!.CredentialTypeId,
            Number = request.Number!.Trim(),
            IssueDate = request.IssueDate!.Value,
            ExpiryDate = request.ExpiryDate!.Value,
        };
        credential.Status = CredentialStatusRules.Derive(CredentialStatus.Valid, credential.ExpiryDate, _clock.Today);
        _credentialRepository.Add(credential);
        await _credentialRepository.SaveChangesAsync();

        return (await _credentialRepository.GetByIdAsync(credential.CredentialId))!.ToDto(_clock.Today);
    }

    /// <summary>Records a renewal (new number and/or dates) or suspends / un-suspends the credential.</summary>
    /// <param name="credentialId">Credential to update.</param>
    /// <param name="request">New number, dates and/or suspension.</param>
    /// <exception cref="Errors.NotFoundException">The credential does not exist.</exception>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    /// @par Business rule 5
    /// The stored status only records Suspended; every other status is derived on read.
    public async Task<CredentialDto> UpdateAsync(int credentialId, UpdateCredentialRequest request)
    {
        var credential = await _credentialRepository.GetByIdAsync(credentialId)
                         ?? throw new NotFoundException("Credential", credentialId);

        var issueDate = request.IssueDate ?? credential.IssueDate;
        var expiryDate = request.ExpiryDate ?? credential.ExpiryDate;

        var errors = new ValidationErrors();
        errors.AddIf(request is { Number: null, IssueDate: null, ExpiryDate: null, Suspended: null }, "body",
            "Provide at least one of number, issueDate, expiryDate or suspended.");
        errors.NotBlankIfPresent("number", request.Number, 50);
        errors.AddIf(issueDate >= expiryDate, "expiryDate", "ExpiryDate must be after IssueDate.");
        errors.ThrowIfAny();

        if (request.Number != null) credential.Number = request.Number.Trim();
        credential.IssueDate = issueDate;
        credential.ExpiryDate = expiryDate;

        var suspended = request.Suspended ?? credential.Status == CredentialStatus.Suspended;
        // The stored status only matters for Suspended; otherwise keep it in line with the derived value.
        credential.Status = suspended
            ? CredentialStatus.Suspended
            : CredentialStatusRules.Derive(CredentialStatus.Valid, expiryDate, _clock.Today);

        await _credentialRepository.SaveChangesAsync();
        return credential.ToDto(_clock.Today);
    }
}
