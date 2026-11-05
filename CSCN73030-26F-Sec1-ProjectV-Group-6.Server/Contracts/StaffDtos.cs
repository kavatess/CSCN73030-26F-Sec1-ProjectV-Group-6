using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

// Business rule 4: no DTO in this file (or anywhere in the API) carries salary or pay band.

/// <summary>Body of POST /staff (the Onboard employee screen).</summary>
/// <param name="EmploymentStatus">Active or Probation. Defaults to Probation.</param>
/// <param name="HomeDepartmentCode">Department code, e.g. EMS.</param>
/// <param name="InitialAssignment">Optional first role, created in the same transaction.</param>
[RequiredFields("firstName", "lastName", "email", "dateOfBirth", "hireDate", "employmentType", "homeDepartmentCode")]
public record CreateStaffRequest(
    string? FirstName,
    string? LastName,
    string? PreferredName,
    string? Email,
    DateOnly? DateOfBirth,
    string? Phone,
    string? CitizenId,
    DateOnly? HireDate,
    EmploymentType? EmploymentType,
    EmploymentStatus? EmploymentStatus,
    string? HomeDepartmentCode,
    CreateAssignmentRequest? InitialAssignment);

/// <summary>
/// Body of PATCH /staff/{staffNumber}. Omit (or send null for) any field you are not changing.
/// Send an empty string to clear PreferredName, Phone or CitizenId.
/// </summary>
/// <param name="StaffNumber">Read-only. Accepted only if it equals the current value; any change is rejected with 422.</param>
/// <param name="EmploymentStatus">Any status except Terminated (termination goes through a termination requisition, not built yet).</param>
/// <param name="HomeDepartmentCode">Moving department never changes the staff number.</param>
public record UpdateStaffRequest(
    string? StaffNumber,
    string? FirstName,
    string? LastName,
    string? PreferredName,
    string? Email,
    DateOnly? DateOfBirth,
    string? Phone,
    string? CitizenId,
    DateOnly? HireDate,
    EmploymentType? EmploymentType,
    EmploymentStatus? EmploymentStatus,
    string? HomeDepartmentCode);

/// <summary>Query string of GET /staff (the Staff directory screen).</summary>
public class StaffSearchQuery
{
    /// <summary>Free text matched against staff number, names and email.</summary>
    public string? Q { get; set; }

    /// <summary>Home department code, e.g. EMS.</summary>
    public string? Department { get; set; }

    /// <summary>Service id; matches staff with a current assignment in that service.</summary>
    public int? Service { get; set; }

    /// <summary>Text matched against the role title of current assignments.</summary>
    public string? Role { get; set; }

    public EmploymentStatus? Status { get; set; }

    /// <summary>Matches staff holding at least one credential in this derived state.</summary>
    public CredentialStatus? CredentialState { get; set; }

    public int Page { get; set; } = 1;

    /// <summary>1 to 100. Defaults to 20.</summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>One row of the staff directory.</summary>
/// <param name="CurrentRole">Role title of the most recent current assignment, if any.</param>
/// <param name="CredentialState">The most urgent credential state (Expired, Suspended, Expiring, Valid), or null if none held.</param>
public record StaffSummaryDto(
    string StaffNumber,
    string DisplayName,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    EmploymentStatus EmploymentStatus,
    EmploymentType EmploymentType,
    DepartmentRefDto HomeDepartment,
    string? CurrentRole,
    string? CurrentService,
    CredentialStatus? CredentialState);

/// <summary>Full employee profile, including assignments and credentials.</summary>
public record StaffProfileDto(
    string StaffNumber,
    string DisplayName,
    string FirstName,
    string LastName,
    string? PreferredName,
    string Email,
    string? Phone,
    DateOnly DateOfBirth,
    string? CitizenId,
    DateOnly HireDate,
    EmploymentStatus EmploymentStatus,
    EmploymentType EmploymentType,
    DepartmentRefDto HomeDepartment,
    CredentialStatus? CredentialState,
    IReadOnlyList<AssignmentDto> Assignments,
    IReadOnlyList<CredentialDto> Credentials);
