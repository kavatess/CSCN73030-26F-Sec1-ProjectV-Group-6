/**
 * @file Credential.cs
 * @brief HR-02 schema draft entity (Credential).
 *
 * Part of the HR-02 schema draft (PR #3). Not registered in Program.cs and not used at runtime: the service uses CmesDbContext and the Domain entities. Its own migration lives in Migrations/; every dotnet ef command names --context CmesDbContext.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A certification or licence held by a person.</summary>
public class Credential
{
    public int CredentialId { get; set; }
    public int StaffId { get; set; }
    public int CredentialTypeId { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public CredentialStatus Status { get; set; }

    public Staff Staff { get; set; } = null!;
    public CredentialType CredentialType { get; set; } = null!;
}
