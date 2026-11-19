/**
 * @file CredentialDtos.cs
 * @brief Request and response shapes for credential endpoints.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of POST /staff/{staffNumber}/credentials.</summary>
/// <param name="CredentialTypeId">Required. Kind of credential.</param>
/// <param name="Number">Required. Licence number, max 50 characters.</param>
/// <param name="IssueDate">Required. Date issued.</param>
/// <param name="ExpiryDate">Required. Last valid day; after IssueDate.</param>
[RequiredFields("credentialTypeId", "number", "issueDate", "expiryDate")]
public record CreateCredentialRequest(int? CredentialTypeId, string? Number, DateOnly? IssueDate, DateOnly? ExpiryDate);

/// <summary>
/// Body of PATCH /credentials/{id}: record a renewal (new dates and/or number), or suspend / lift a suspension.
/// Omit any field you are not changing; at least one is required.
/// </summary>
/// <param name="Number">New licence number.</param>
/// <param name="IssueDate">New issue date.</param>
/// <param name="ExpiryDate">New expiry date; must stay after the issue date.</param>
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

    /// <summary>1-based page number. Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>1 to 100. Defaults to 20.</summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>A credential as returned by the API.</summary>
/// <param name="CredentialId">Id used by PATCH /credentials/{id}.</param>
/// <param name="StaffNumber">Holder's staff number.</param>
/// <param name="StaffName">Holder's display name.</param>
/// <param name="DepartmentCode">Holder's home department code.</param>
/// <param name="CredentialTypeId">Kind of credential.</param>
/// <param name="CredentialType">Name of the kind of credential.</param>
/// <param name="IsDutyBlocking">Whether an expired or suspended credential of this type blocks shifts.</param>
/// <param name="Number">Licence number.</param>
/// <param name="IssueDate">Date issued.</param>
/// <param name="ExpiryDate">Last valid day.</param>
/// <param name="Status">Derived on read from ExpiryDate unless Suspended (business rule 5).</param>
/// <param name="DaysUntilExpiry">Days from today to ExpiryDate; negative once expired.</param>
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

/// <summary>A kind of credential, from GET /credentials/types.</summary>
/// <param name="CredentialTypeId">Id to send as credentialTypeId.</param>
/// <param name="Name">Display name, e.g. Primary Care Paramedic Licence.</param>
/// <param name="IsDutyBlocking">When true, an expired or suspended credential of this type makes the holder ineligible for duty.</param>
public record CredentialTypeDto(int CredentialTypeId, string Name, bool IsDutyBlocking);
