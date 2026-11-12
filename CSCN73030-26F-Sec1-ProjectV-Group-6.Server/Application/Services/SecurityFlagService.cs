/**
 * @file SecurityFlagService.cs
 * @brief Keeps security flags in step with the Security service.
 */

using System.Text.RegularExpressions;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>
/// Security-flag sync. The Security service pushes each flag change with PUT; the change is an
/// upsert keyed by staff member and flag type, and an update older than the stored one is ignored,
/// so retried or out-of-order messages are harmless.
/// </summary>
public partial class SecurityFlagService
{
    /// <summary>What an upsert did.</summary>
    public enum UpsertOutcome
    {
        /// <summary>A new flag was recorded.</summary>
        Created,
        /// <summary>The existing flag was updated.</summary>
        Updated,
        /// <summary>The update was older than the stored flag and was ignored.</summary>
        IgnoredAsStale
    }

    /// <summary>Result of an upsert: the flag as now stored, and what happened.</summary>
    /// <param name="Flag">The stored flag.</param>
    /// <param name="Outcome">Created, Updated or IgnoredAsStale.</param>
    public record UpsertResult(SecurityFlagDto Flag, UpsertOutcome Outcome);

    private readonly SecurityFlagRepository _flagRepository;
    private readonly StaffRepository _staffRepository;
    private readonly Clock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="flagRepository">Security flag data access.</param>
    /// <param name="staffRepository">Staff data access.</param>
    /// <param name="clock">Current UTC time.</param>
    public SecurityFlagService(SecurityFlagRepository flagRepository, StaffRepository staffRepository, Clock clock)
    {
        _flagRepository = flagRepository;
        _staffRepository = staffRepository;
        _clock = clock;
    }

    /// <summary>Creates or updates one flag of a staff member.</summary>
    /// <param name="staffNumber">Flagged staff member.</param>
    /// <param name="flagType">Kind of flag.</param>
    /// <param name="request">New status and when it changed.</param>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    /// <exception cref="Errors.ValidationFailedException">The flag type or status is invalid (422).</exception>
    public async Task<UpsertResult> UpsertAsync(string staffNumber, string flagType, UpsertSecurityFlagRequest request)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);

        var errors = new ValidationErrors();
        errors.AddIf(!FlagTypePattern().IsMatch(flagType), "flagType",
            "Flag type must be 1-50 letters, digits, '.', '-' or '_'.");
        errors.Required("status", request.Status);
        errors.ThrowIfAny();

        var changedAt = request.UpdatedAt is { } at ? Clock.AsUtc(at) : _clock.UtcNow;
        var flag = await _flagRepository.FindAsync(staff.StaffId, flagType);

        if (flag == null)
        {
            flag = new SecurityFlag
            {
                StaffId = staff.StaffId,
                Staff = staff,
                FlagType = flagType,
                Status = request.Status!.Value,
                UpdatedAt = changedAt,
            };
            _flagRepository.Add(flag);
            await _flagRepository.SaveChangesAsync();
            return new UpsertResult(ToDto(flag), UpsertOutcome.Created);
        }

        if (changedAt < flag.UpdatedAt)
            return new UpsertResult(ToDto(flag), UpsertOutcome.IgnoredAsStale);

        flag.Status = request.Status!.Value;
        flag.UpdatedAt = changedAt;
        await _flagRepository.SaveChangesAsync();
        return new UpsertResult(ToDto(flag), UpsertOutcome.Updated);
    }

    /// <summary>Every flag of one staff member.</summary>
    /// <param name="staffNumber">Staff member.</param>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    public async Task<List<SecurityFlagDto>> ListForStaffAsync(string staffNumber)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);
        return (await _flagRepository.ListForStaffAsync(staff.StaffId)).Select(ToDto).ToList();
    }

    /// <summary>Flags across the city, most recently changed first.</summary>
    /// <param name="query">Optional status and flag type.</param>
    public async Task<List<SecurityFlagDto>> SearchAsync(SecurityFlagQuery query) =>
        (await _flagRepository.SearchAsync(query.Status, Text.TrimToNull(query.FlagType))).Select(ToDto).ToList();

    private static SecurityFlagDto ToDto(SecurityFlag flag) =>
        new(flag.FlagId, flag.Staff.StaffNumber, flag.FlagType, flag.Status, flag.UpdatedAt);

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,50}$")]
    private static partial Regex FlagTypePattern();
}
