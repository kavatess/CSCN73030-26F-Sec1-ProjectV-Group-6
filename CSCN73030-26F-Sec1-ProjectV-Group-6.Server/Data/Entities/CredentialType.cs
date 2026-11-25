/**
 * @file CredentialType.cs
 * @brief HR-02 schema draft entity (CredentialType).
 *
 * Part of the HR-02 schema draft (PR #3). Not registered in Program.cs and not used at runtime: the service uses CmesDbContext and the Domain entities. Its own migration lives in Migrations/; every dotnet ef command names --context CmesDbContext.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A kind of certification or licence (paramedic, heavy machinery, ...).</summary>
public class CredentialType
{
    public int CredentialTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
}
