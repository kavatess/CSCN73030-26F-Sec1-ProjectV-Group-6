namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A licence or certification held by a staff member.</summary>
public class Credential
{
    public int CredentialId { get; set; }
    public int StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public int CredentialTypeId { get; set; }
    public CredentialType CredentialType { get; set; } = null!;
    public string Number { get; set; } = "";
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }

    /// <summary>
    /// The stored status. Only <see cref="CredentialStatus.Suspended"/> is meaningful here;
    /// every other value is recomputed from ExpiryDate on read. Always read the status through
    /// <see cref="CredentialStatusRules.Derive(Credential, DateOnly)"/>.
    /// </summary>
    public CredentialStatus Status { get; set; }
}
