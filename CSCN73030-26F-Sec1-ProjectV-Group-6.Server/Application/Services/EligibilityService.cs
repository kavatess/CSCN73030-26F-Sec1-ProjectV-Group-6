/**
 * @file EligibilityService.cs
 * @brief Answers "may this person work a shift?" for peer services.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>Loads what <see cref="DutyEligibilityRules"/> needs and returns its decision.</summary>
public class EligibilityService
{
    private readonly StaffRepository _staffRepository;
    private readonly SecurityFlagRepository _flagRepository;
    private readonly DepartmentRepository _departmentRepository;
    private readonly Clock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="staffRepository">Staff data access.</param>
    /// <param name="flagRepository">Security flag data access.</param>
    /// <param name="departmentRepository">Service lookups.</param>
    /// <param name="clock">Current UTC time.</param>
    public EligibilityService(StaffRepository staffRepository, SecurityFlagRepository flagRepository,
        DepartmentRepository departmentRepository, Clock clock)
    {
        _staffRepository = staffRepository;
        _flagRepository = flagRepository;
        _departmentRepository = departmentRepository;
        _clock = clock;
    }

    /// <summary>Checks a staff member's eligibility for duty.</summary>
    /// <param name="staffNumber">Staff member.</param>
    /// <param name="serviceId">Service the shift is in, or null for any service.</param>
    /// <param name="at">Instant to check, or null for now.</param>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    /// <exception cref="Errors.ValidationFailedException">The service does not exist (422).</exception>
    public async Task<DutyEligibilityDto> CheckAsync(string staffNumber, int? serviceId, DateTime? at)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber, withDetails: true)
                    ?? throw new NotFoundException("Staff", staffNumber);

        if (serviceId != null && await _departmentRepository.GetServiceAsync(serviceId.Value) == null)
            throw new ValidationFailedException("service", $"Unknown service id {serviceId}.");

        var instant = at != null ? Clock.AsUtc(at.Value) : _clock.UtcNow;
        var flags = await _flagRepository.ListForStaffAsync(staff.StaffId);
        var decision = DutyEligibilityRules.Evaluate(staff, flags, DateOnly.FromDateTime(instant), serviceId);

        return new DutyEligibilityDto(
            staff.StaffNumber,
            decision.Eligible,
            instant,
            serviceId,
            decision.Reasons.Select(r => new EligibilityIssueDto(r.Code, r.Message)).ToList(),
            decision.Warnings.Select(w => new EligibilityIssueDto(w.Code, w.Message)).ToList());
    }
}
