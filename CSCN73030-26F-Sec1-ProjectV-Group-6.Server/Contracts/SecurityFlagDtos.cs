/**
 * @file SecurityFlagDtos.cs
 * @brief Request and response shapes for security flag sync.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of PUT /staff/{staffNumber}/security-flags/{flagType}, sent by the Security service.</summary>
/// <param name="Status">Required. Active (blocks duty) or Cleared.</param>
/// <param name="UpdatedAt">When the Security service changed the flag (UTC). An update older than the stored one is ignored. Defaults to now.</param>
[RequiredFields("status")]
public record UpsertSecurityFlagRequest(SecurityFlagStatus? Status, DateTime? UpdatedAt);

/// <summary>A security flag as returned by the API.</summary>
/// <param name="FlagId">Flag id.</param>
/// <param name="StaffNumber">Flagged staff member.</param>
/// <param name="FlagType">Kind of flag, as named by the Security service.</param>
/// <param name="Status">Active or Cleared.</param>
/// <param name="UpdatedAt">When the Security service last changed it, UTC.</param>
public record SecurityFlagDto(int FlagId, string StaffNumber, string FlagType, SecurityFlagStatus Status, DateTime UpdatedAt);

/// <summary>Query string of GET /security-flags.</summary>
public class SecurityFlagQuery
{
    /// <summary>Active or Cleared.</summary>
    public SecurityFlagStatus? Status { get; set; }

    /// <summary>Flag type, e.g. UnderInvestigation.</summary>
    public string? FlagType { get; set; }
}
