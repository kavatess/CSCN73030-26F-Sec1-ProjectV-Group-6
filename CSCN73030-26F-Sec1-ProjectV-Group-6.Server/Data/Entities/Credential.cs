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
