/**
 * @file Staff.cs
 * @brief Staff entity: one municipal employee.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A municipal employee. The staff number is the key other services store.</summary>
public class Staff
{
    /// <summary>Database key. Internal; the API identifies staff by <see cref="StaffNumber"/>.</summary>
    public int StaffId { get; set; }

    /// <summary>
    /// STF-0000000. Allocated once from a city-wide sequence and never changed or reused.
    /// EF is configured to throw if code tries to modify it after the first save.
    /// </summary>
    /// @par Business rule 1
    /// Immutable, sequence-allocated, never reused, no department code.
    public string StaffNumber { get; set; } = "";

    /// <summary>Legal first name.</summary>
    public string FirstName { get; set; } = "";

    /// <summary>Legal last name.</summary>
    public string LastName { get; set; } = "";

    /// <summary>Name the employee goes by, if different from <see cref="FirstName"/>.</summary>
    public string? PreferredName { get; set; }

    /// <summary>Work email, unique across the city. Stored lower-case.</summary>
    /// @par Business rule 2
    /// A duplicate email is rejected with 409 Conflict.
    public string Email { get; set; } = "";

    /// <summary>Date of birth. Must be in the past and at least 16 years before <see cref="HireDate"/>.</summary>
    public DateOnly DateOfBirth { get; set; }

    /// <summary>Work phone number, if any.</summary>
    public string? Phone { get; set; }

    /// <summary>Resident identifier from the Residence service, if known.</summary>
    public string? CitizenId { get; set; }

    /// <summary>First day of employment.</summary>
    public DateOnly HireDate { get; set; }

    /// <summary>Current employment status.</summary>
    public EmploymentStatus EmploymentStatus { get; set; }

    /// <summary>Contract type.</summary>
    public EmploymentType EmploymentType { get; set; }

    /// <summary>Home department's key. Changing it (a transfer) never changes the staff number.</summary>
    public int HomeDepartmentId { get; set; }

    /// <summary>Home department.</summary>
    public Department HomeDepartment { get; set; } = null!;

    /// <summary>Roles held over time, current and past.</summary>
    public List<StaffAssignment> Assignments { get; set; } = [];

    /// <summary>Licences and certifications held.</summary>
    public List<Credential> Credentials { get; set; } = [];

    /// <summary>Preferred name if set, otherwise first name, followed by last name. Not stored.</summary>
    public string DisplayName => $"{PreferredName ?? FirstName} {LastName}";
}
