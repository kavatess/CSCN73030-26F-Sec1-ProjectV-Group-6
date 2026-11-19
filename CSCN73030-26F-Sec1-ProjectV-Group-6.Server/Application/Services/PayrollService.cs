/**
 * @file PayrollService.cs
 * @brief Pay history and payroll headcount for the Budget service.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>
/// The only place salary is read or written. Rates form a non-overlapping history: a new rate closes
/// the current one on its EffectiveFrom (EffectiveTo is exclusive).
/// </summary>
/// @par Business rule 4
/// Salary is returned only here, never by staff or search endpoints.
public class PayrollService
{
    /// <summary>Highest annual salary accepted, as a guard against typing errors.</summary>
    public const decimal MaxAnnualSalary = 1_000_000m;

    private readonly CompensationRepository _compensationRepository;
    private readonly StaffRepository _staffRepository;
    private readonly DepartmentRepository _departmentRepository;
    private readonly Clock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="compensationRepository">Pay history data access.</param>
    /// <param name="staffRepository">Staff data access.</param>
    /// <param name="departmentRepository">Department data access.</param>
    /// <param name="clock">Current UTC time.</param>
    public PayrollService(CompensationRepository compensationRepository, StaffRepository staffRepository,
        DepartmentRepository departmentRepository, Clock clock)
    {
        _compensationRepository = compensationRepository;
        _staffRepository = staffRepository;
        _departmentRepository = departmentRepository;
        _clock = clock;
    }

    /// <summary>
    /// Records a new rate from EffectiveFrom and closes the current one on that day. A rate starting
    /// on the same day as the current one replaces it (a correction).
    /// </summary>
    /// <param name="staffNumber">Employee.</param>
    /// <param name="request">Pay band, salary and start date.</param>
    /// <returns>The full pay history after the change, newest first.</returns>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    /// <exception cref="Errors.ValidationFailedException">A field breaks a rule (422).</exception>
    public async Task<List<CompensationDto>> SetAsync(string staffNumber, SetCompensationRequest request)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);
        var history = await _compensationRepository.ListForStaffAsync(staff.StaffId);
        var current = history.FirstOrDefault(c => c.EffectiveTo == null);

        var errors = new ValidationErrors();
        errors.Required("payBand", request.PayBand, 20);
        errors.Required("annualSalary", request.AnnualSalary);
        errors.AddIf(request.AnnualSalary is <= 0 or > MaxAnnualSalary, "annualSalary",
            $"AnnualSalary must be more than 0 and at most {MaxAnnualSalary:N0}.");
        errors.Required("effectiveFrom", request.EffectiveFrom);
        errors.AddIf(staff.EmploymentStatus == EmploymentStatus.Terminated, "staffNumber", "A terminated employee has no new pay.");
        errors.AddIf(request.EffectiveFrom < staff.HireDate, "effectiveFrom", "A rate cannot start before the hire date.");
        errors.AddIf(current != null && request.EffectiveFrom < current.EffectiveFrom, "effectiveFrom",
            $"A new rate must start on or after the current rate's start ({current?.EffectiveFrom:yyyy-MM-dd}).");
        errors.ThrowIfAny();

        var from = request.EffectiveFrom!.Value;
        if (current != null && current.EffectiveFrom == from)
        {
            current.PayBand = request.PayBand!.Trim();
            current.AnnualSalary = request.AnnualSalary!.Value;
        }
        else
        {
            if (current != null) current.EffectiveTo = from;
            _compensationRepository.Add(new Compensation
            {
                StaffId = staff.StaffId,
                Staff = staff,
                PayBand = request.PayBand!.Trim(),
                AnnualSalary = request.AnnualSalary!.Value,
                EffectiveFrom = from,
            });
        }
        await _compensationRepository.SaveChangesAsync();

        return await HistoryAsync(staff.StaffId);
    }

    /// <summary>An employee's pay history, newest first.</summary>
    /// <param name="staffNumber">Employee.</param>
    /// <exception cref="Errors.NotFoundException">The staff member does not exist.</exception>
    public async Task<List<CompensationDto>> ListAsync(string staffNumber)
    {
        var staff = await _staffRepository.GetByNumberAsync(staffNumber)
                    ?? throw new NotFoundException("Staff", staffNumber);
        return await HistoryAsync(staff.StaffId);
    }

    /// <summary>Payroll headcount and salary totals per department, for the Budget service.</summary>
    /// <param name="asOf">Payroll date, or null for today.</param>
    public async Task<PayrollHeadcountDto> HeadcountAsync(DateOnly? asOf)
    {
        var date = asOf ?? _clock.Today;
        var departments = await _departmentRepository.ListWithServicesAsync();
        var figures = (await _compensationRepository.PayrollByDepartmentAsync(date)).ToDictionary(f => f.DepartmentId);

        var rows = departments.Select(d =>
        {
            var f = figures.GetValueOrDefault(d.DepartmentId) ?? new DepartmentPayroll(d.DepartmentId, 0, 0, 0, 0, 0m);
            var average = f.WithCompensation == 0 ? 0m : Math.Round(f.TotalAnnualSalary / f.WithCompensation, 2);
            return new DepartmentPayrollDto(d.Code, d.Name, f.Headcount, f.FullTime, f.PartTime,
                f.TotalAnnualSalary, average, f.Headcount - f.WithCompensation);
        }).ToList();

        return new PayrollHeadcountDto(date, rows, rows.Sum(r => r.Headcount), rows.Sum(r => r.TotalAnnualSalary));
    }

    private async Task<List<CompensationDto>> HistoryAsync(int staffId) =>
        (await _compensationRepository.ListForStaffAsync(staffId))
            .Select(c => new CompensationDto(c.CompensationId, c.Staff.StaffNumber, c.PayBand, c.AnnualSalary, c.EffectiveFrom, c.EffectiveTo))
            .ToList();
}
