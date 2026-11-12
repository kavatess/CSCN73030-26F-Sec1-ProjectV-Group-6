/**
 * @file Credential.cs
 * @brief Credential entity: a licence or certification held by a staff member.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A licence or certification held by a staff member.</summary>
/// @par Business rule 6
/// IssueDate must be before ExpiryDate (validated in CredentialService and by a check constraint).
public class Credential
{
    /// <summary>Database key, also the id used by <c>PATCH /credentials/{id}</c>.</summary>
    public int CredentialId { get; set; }

    /// <summary>Holder's key.</summary>
    public int StaffId { get; set; }

    /// <summary>Holder.</summary>
    public Staff Staff { get; set; } = null!;

    /// <summary>Kind of credential.</summary>
    public int CredentialTypeId { get; set; }

    /// <summary>Kind of credential.</summary>
    public CredentialType CredentialType { get; set; } = null!;

    /// <summary>Licence or certificate number issued by the awarding body.</summary>
    public string Number { get; set; } = "";

    /// <summary>Date issued.</summary>
    public DateOnly IssueDate { get; set; }

    /// <summary>Last valid day. Drives the derived status.</summary>
    public DateOnly ExpiryDate { get; set; }

    /// <summary>
    /// The stored status. Only <see cref="CredentialStatus.Suspended"/> is meaningful here;
    /// every other value is recomputed from ExpiryDate on read. Always read the status through
    /// <see cref="CredentialStatusRules.Derive(Credential, DateOnly)"/>.
    /// </summary>
    public CredentialStatus Status { get; set; }
}
