/**
 * @file EligibilityDtos.cs
 * @brief Response shape of GET /staff/{staffNumber}/eligibility.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>One reason or warning.</summary>
/// <param name="Code">EMPLOYMENT_STATUS, NO_CURRENT_ASSIGNMENT, CREDENTIAL_EXPIRED, CREDENTIAL_SUSPENDED, SECURITY_FLAG_ACTIVE or (warning) CREDENTIAL_EXPIRING.</param>
/// <param name="Message">Explanation for people.</param>
public record EligibilityIssueDto(string Code, string Message);

/// <summary>Whether a staff member may work a shift.</summary>
/// <param name="StaffNumber">Staff member checked.</param>
/// <param name="Eligible">True when nothing blocks duty.</param>
/// <param name="At">Instant checked (UTC); its date is used for assignments and credentials.</param>
/// <param name="ServiceId">Service checked, or null for any service.</param>
/// <param name="Reasons">Why not eligible; empty when eligible.</param>
/// <param name="Warnings">Non-blocking issues, e.g. a duty-blocking credential expiring soon.</param>
public record DutyEligibilityDto(
    string StaffNumber,
    bool Eligible,
    DateTime At,
    int? ServiceId,
    IReadOnlyList<EligibilityIssueDto> Reasons,
    IReadOnlyList<EligibilityIssueDto> Warnings);
