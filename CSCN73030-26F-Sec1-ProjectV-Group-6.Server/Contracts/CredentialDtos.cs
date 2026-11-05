using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of POST /staff/{staffNumber}/credentials.</summary>
[RequiredFields("credentialTypeId", "number", "issueDate", "expiryDate")]
public record CreateCredentialRequest(int? CredentialTypeId, string? Number, DateOnly? IssueDate, DateOnly? ExpiryDate);

/// <summary>
/// Body of PATCH /credentials/{id}: record a renewal (new dates and/or number), or suspend / lift a suspension.
/// Omit any field you are not changing.
/// </summary>
/// <param name="Suspended">true suspends the credential; false lifts the suspension so status is derived from ExpiryDate again.</param>
public record UpdateCredentialRequest(string? Number, DateOnly? IssueDate, DateOnly? ExpiryDate, bool? Suspended);

/// <summary>Query string of GET /credentials.</summary>
public class CredentialQuery
{
    /// <summary>Derived status: Valid, Expiring, Expired or Suspended.</summary>
    public CredentialStatus? State { get; set; }

    /// <summary>CredentialTypeId.</summary>
    public int? Type { get; set; }

    /// <summary>Holder's home department code, e.g. EMS.</summary>
    public string? Department { get; set; }

    public int Page { get; set; } = 1;

    /// <summary>1 to 100. Defaults to 20.</summary>
    public int PageSize { get; set; } = 20;
}

/// <param name="Status">Derived on read from ExpiryDate unless Suspended (business rule 5).</param>
/// <param name="DaysUntilExpiry">Negative once expired.</param>
public record CredentialDto(
    int CredentialId,
    string StaffNumber,
    string StaffName,
    string DepartmentCode,
    int CredentialTypeId,
    string CredentialType,
    bool IsDutyBlocking,
    string Number,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    CredentialStatus Status,
    int DaysUntilExpiry);
