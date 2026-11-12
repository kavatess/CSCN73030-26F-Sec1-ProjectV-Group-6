/**
 * @file StaffDtos.cs
 * @brief Request and response shapes for the /staff endpoints.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

// Business rule 4: no DTO in this file (or anywhere in the API) carries salary or pay band.

/// <summary>Body of POST /staff (the Onboard employee screen).</summary>
/// <param name="FirstName">Required. Legal first name, max 100 characters.</param>
/// <param name="LastName">Required. Legal last name, max 100 characters.</param>
/// <param name="PreferredName">Optional name the employee goes by.</param>
/// <param name="Email">Required. Work email, unique city-wide (case-insensitive). A duplicate is 409.</param>
/// <param name="DateOfBirth">Required. Must be in the past.</param>
/// <param name="Phone">Optional work phone, max 30 characters.</param>
/// <param name="CitizenId">Optional Residence-service id, max 30 characters.</param>
/// <param name="HireDate">Required. At least 16 years after DateOfBirth.</param>
/// <param name="EmploymentType">Required. FullTime, PartTime, Contract or Seasonal.</param>
/// <param name="EmploymentStatus">Active or Probation. Defaults to Probation.</param>
/// <param name="HomeDepartmentCode">Required. Department code, e.g. EMS.</param>
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
/// <param name="FirstName">New first name; may not be blank.</param>
/// <param name="LastName">New last name; may not be blank.</param>
/// <param name="PreferredName">New preferred name; "" clears it.</param>
/// <param name="Email">New work email; must be unique city-wide (409 if taken).</param>
/// <param name="DateOfBirth">Corrected date of birth.</param>
/// <param name="Phone">New phone; "" clears it.</param>
/// <param name="CitizenId">New citizen id; "" clears it.</param>
/// <param name="HireDate">Corrected hire date.</param>
/// <param name="EmploymentType">New contract type.</param>
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

    /// <summary>Employment status to match.</summary>
    public EmploymentStatus? Status { get; set; }

    /// <summary>Matches staff holding at least one credential in this derived state.</summary>
    public CredentialStatus? CredentialState { get; set; }

    /// <summary>1-based page number. Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>1 to 100. Defaults to 20.</summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>One row of the staff directory.</summary>
/// <param name="StaffNumber">STF-0000000.</param>
/// <param name="DisplayName">Preferred (or first) name plus last name.</param>
/// <param name="FirstName">Legal first name.</param>
/// <param name="LastName">Legal last name.</param>
/// <param name="Email">Work email.</param>
/// <param name="Phone">Work phone, or null.</param>
/// <param name="EmploymentStatus">Current employment status.</param>
/// <param name="EmploymentType">Contract type.</param>
/// <param name="HomeDepartment">Home department.</param>
/// <param name="CurrentRole">Role title of the most recent current assignment, if any.</param>
/// <param name="CurrentService">Service of that assignment, if any.</param>
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

/// <summary>Full employee profile, including assignments and credentials. Never includes salary.</summary>
/// <param name="StaffNumber">STF-0000000.</param>
/// <param name="DisplayName">Preferred (or first) name plus last name.</param>
/// <param name="FirstName">Legal first name.</param>
/// <param name="LastName">Legal last name.</param>
/// <param name="PreferredName">Preferred name, or null.</param>
/// <param name="Email">Work email.</param>
/// <param name="Phone">Work phone, or null.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="CitizenId">Residence-service id, or null.</param>
/// <param name="HireDate">First day of employment.</param>
/// <param name="EmploymentStatus">Current employment status.</param>
/// <param name="EmploymentType">Contract type.</param>
/// <param name="HomeDepartment">Home department.</param>
/// <param name="CredentialState">The most urgent credential state, or null if none held.</param>
/// <param name="Assignments">Every assignment, current ones first, then newest first.</param>
/// <param name="Credentials">Every credential, soonest expiry first.</param>
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
