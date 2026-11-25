/**
 * @file Staff.cs
 * @brief HR-02 schema draft entity (Staff).
 *
 * Part of the HR-02 schema draft (PR #3). Not registered in Program.cs and not used at runtime: the service uses CmesDbContext and the Domain entities. Its own migration lives in Migrations/; every dotnet ef command names --context CmesDbContext.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A person employed by the city. Never deleted: a termination changes EmploymentStatus.</summary>
public class Staff
{
    public int StaffId { get; set; }
    public string StaffNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public EmploymentStatus EmploymentStatus { get; set; }

    public List<StaffAssignment> Assignments { get; set; } = new();
    public List<RosterEntry> RosterEntries { get; set; } = new();
    public List<Credential> Credentials { get; set; } = new();
    public List<SecurityFlag> SecurityFlags { get; set; } = new();
    public List<Compensation> Compensations { get; set; } = new();
}
