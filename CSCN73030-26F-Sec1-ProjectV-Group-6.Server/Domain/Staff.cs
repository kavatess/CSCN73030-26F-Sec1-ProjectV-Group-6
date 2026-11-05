namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A municipal employee. The staff number is the key other services store.</summary>
public class Staff
{
    public int StaffId { get; set; }

    /// <summary>
    /// STF-0000000. Allocated once from a city-wide sequence and never changed or reused.
    /// EF is configured to throw if code tries to modify it after the first save.
    /// </summary>
    public string StaffNumber { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PreferredName { get; set; }

    /// <summary>Work email, unique across the city. Stored lower-case.</summary>
    public string Email { get; set; } = "";

    public DateOnly DateOfBirth { get; set; }
    public string? Phone { get; set; }

    /// <summary>Resident identifier from the Residence service, if known.</summary>
    public string? CitizenId { get; set; }

    public DateOnly HireDate { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }
    public EmploymentType EmploymentType { get; set; }

    public int HomeDepartmentId { get; set; }
    public Department HomeDepartment { get; set; } = null!;

    public List<StaffAssignment> Assignments { get; set; } = [];
    public List<Credential> Credentials { get; set; } = [];

    /// <summary>Preferred name if set, otherwise first name, followed by last name.</summary>
    public string DisplayName => $"{PreferredName ?? FirstName} {LastName}";
}
