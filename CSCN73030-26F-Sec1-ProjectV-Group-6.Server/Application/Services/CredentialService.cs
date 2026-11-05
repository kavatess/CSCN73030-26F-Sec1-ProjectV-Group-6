using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Licences and certifications. Status is always derived on read (business rule 5).</summary>
public class CredentialService(
    CredentialRepository credentialRepository,
    StaffRepository staffRepository,
    DepartmentRepository departmentRepository,
    Clock clock)
{
    public const int MaxWithinDays = 365;

    public async Task<PagedResult<CredentialDto>> SearchAsync(CredentialQuery query)
    {
        var errors = new ValidationErrors();
        StaffService.ValidatePaging(errors, query.Page, query.PageSize);

        int? departmentId = null;
        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            var department = await departmentRepository.GetByCodeAsync(query.Department);
            errors.AddIf(department == null, "department", $"Unknown department code '{query.Department}'.");
            departmentId = department?.DepartmentId;
        }
        errors.ThrowIfAny();

        var today = clock.Today;
        var (items, total) = await credentialRepository.SearchAsync(
            query.State, query.Type, departmentId, today, query.Page, query.PageSize);
        return new PagedResult<CredentialDto>(items.Select(c => c.ToDto(today)).ToList(), query.Page, query.PageSize, total);
    }

    /// <summary>Credentials (not suspended, not yet expired) whose expiry falls in the next <paramref name="withinDays"/> days.</summary>
    public async Task<List<CredentialDto>> ExpiringAsync(int withinDays)
    {
        if (withinDays is < 0 or > MaxWithinDays)
            throw new ValidationFailedException("withinDays", $"withinDays must be between 0 and {MaxWithinDays}.");

        var today = clock.Today;
        var credentials = await credentialRepository.ExpiringAsync(today, today.AddDays(withinDays));
        return credentials.Select(c => c.ToDto(today)).ToList();
    }

    public async Task<CredentialDto> CreateAsync(string staffNumber, CreateCredentialRequest request)
    {
        var staff = await staffRepository.GetByNumberAsync(staffNumber)
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
            type = await credentialRepository.GetTypeAsync(request.CredentialTypeId.Value);
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
        credential.Status = CredentialStatusRules.Derive(CredentialStatus.Valid, credential.ExpiryDate, clock.Today);
        credentialRepository.Add(credential);
        await credentialRepository.SaveChangesAsync();

        return (await credentialRepository.GetByIdAsync(credential.CredentialId))!.ToDto(clock.Today);
    }

    /// <summary>Records a renewal (new number and/or dates) or suspends / un-suspends the credential.</summary>
    public async Task<CredentialDto> UpdateAsync(int credentialId, UpdateCredentialRequest request)
    {
        var credential = await credentialRepository.GetByIdAsync(credentialId)
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
            : CredentialStatusRules.Derive(CredentialStatus.Valid, expiryDate, clock.Today);

        await credentialRepository.SaveChangesAsync();
        return credential.ToDto(clock.Today);
    }
}
