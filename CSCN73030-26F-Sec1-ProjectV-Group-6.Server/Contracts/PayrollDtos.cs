/**
 * @file PayrollDtos.cs
 * @brief Request and response shapes for compensation and payroll headcount (payroll role only).
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>Body of PUT /staff/{staffNumber}/compensation: a new rate from a date.</summary>
/// <param name="PayBand">Required. Pay band code, e.g. EMS-4, max 20 characters.</param>
/// <param name="AnnualSalary">Required. Annual salary in dollars, more than 0 and at most 1,000,000.</param>
/// <param name="EffectiveFrom">Required. First day of the new rate. Must not be before the current rate's start; the same day corrects the current rate.</param>
[RequiredFields("payBand", "annualSalary", "effectiveFrom")]
public record SetCompensationRequest(string? PayBand, decimal? AnnualSalary, DateOnly? EffectiveFrom);

/// <summary>One rate in an employee's pay history.</summary>
/// <param name="CompensationId">Rate id.</param>
/// <param name="StaffNumber">Employee.</param>
/// <param name="PayBand">Pay band code.</param>
/// <param name="AnnualSalary">Annual salary in dollars.</param>
/// <param name="EffectiveFrom">First day the rate applies.</param>
/// <param name="EffectiveTo">First day it no longer applies (exclusive), or null while current.</param>
public record CompensationDto(int CompensationId, string StaffNumber, string PayBand, decimal AnnualSalary,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo);

/// <summary>Payroll figures for one department, for the Budget service.</summary>
/// <param name="Code">Department code.</param>
/// <param name="Name">Department name.</param>
/// <param name="Headcount">Employees on payroll: hired by the date and not terminated.</param>
/// <param name="FullTime">Of those, FullTime.</param>
/// <param name="PartTime">Of those, PartTime, Contract or Seasonal.</param>
/// <param name="TotalAnnualSalary">Sum of the rates in force on the date.</param>
/// <param name="AverageAnnualSalary">Average of the rates in force, or 0 when none.</param>
/// <param name="MissingCompensation">Employees on payroll with no rate in force (a data gap to fix).</param>
public record DepartmentPayrollDto(string Code, string Name, int Headcount, int FullTime, int PartTime,
    decimal TotalAnnualSalary, decimal AverageAnnualSalary, int MissingCompensation);

/// <summary>Response of GET /headcounts/payroll.</summary>
/// <param name="AsOf">Payroll date.</param>
/// <param name="Departments">One row per department, by code.</param>
/// <param name="TotalHeadcount">City-wide employees on payroll.</param>
/// <param name="TotalAnnualSalary">City-wide sum of rates in force.</param>
public record PayrollHeadcountDto(DateOnly AsOf, IReadOnlyList<DepartmentPayrollDto> Departments, int TotalHeadcount, decimal TotalAnnualSalary);
