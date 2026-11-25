/**
 * @file BudgetLedgerService.cs
 * @brief Department budgets read from Budget and Treasury, and monthly salary cost statements sent to it.
 */

using System.Globalization;
using System.Text.RegularExpressions;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Repositories;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Budget;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Services;

/// <summary>
/// The money side of the Budget integration: a department's allocation and spend (contract HR-C18), and the
/// monthly salary cost per department, recorded in Budget as an expense (HR-C20). Statements carry department
/// totals only, never individual salaries.
/// </summary>
public partial class BudgetLedgerService
{
    private readonly BudgetClient _budget;
    private readonly DepartmentRepository _departmentRepository;
    private readonly PayrollStatementRepository _statementRepository;
    private readonly PayrollService _payrollService;
    private readonly ICurrentUser _currentUser;
    private readonly Clock _clock;

    /// <summary>Creates the service.</summary>
    public BudgetLedgerService(BudgetClient budget, DepartmentRepository departmentRepository,
        PayrollStatementRepository statementRepository, PayrollService payrollService, ICurrentUser currentUser, Clock clock)
    {
        _budget = budget;
        _departmentRepository = departmentRepository;
        _statementRepository = statementRepository;
        _payrollService = payrollService;
        _currentUser = currentUser;
        _clock = clock;
    }

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$")]
    private static partial Regex PeriodPattern();

    /// <summary>A department's budget from Budget (GET /api/budget/v1/budgets/{code}).</summary>
    /// <exception cref="NotFoundException">No such department here, or Budget has none under the mapped code (404).</exception>
    /// <exception cref="BudgetServiceException">Budget failed (502).</exception>
    public async Task<DepartmentBudgetDto> DepartmentBudgetAsync(string code)
    {
        var department = await _departmentRepository.GetByCodeAsync(code) ?? throw new NotFoundException("Department", code);
        var budgetCode = _budget.ToBudgetCode(department.Code);
        var budget = await _budget.GetDepartmentBudgetAsync(budgetCode)
                     ?? throw new NotFoundException("Budget for department", $"{department.Code} (Budget code {budgetCode})");
        return new DepartmentBudgetDto(department.Code, department.Name, budgetCode, budget.Allocated, budget.Spent,
            budget.Remaining, budget.LowBudget);
    }

    /// <summary>The statements already sent for a month (default: this month).</summary>
    /// <exception cref="ValidationFailedException">The period is not "yyyy-MM" (422).</exception>
    public async Task<PayrollStatementsDto> ListStatementsAsync(string? period)
    {
        var month = ParsePeriod(period);
        var statements = await _statementRepository.ListForPeriodAsync(month);
        return ToDto(month, AsOf(month), statements, statements.ToDictionary(s => s.DepartmentId, s => s.Department.Code));
    }

    /// <summary>
    /// Sends each department's salary cost for a month to Budget as an expense: the annual salaries on payroll at
    /// the month's end (or today, for the current month) divided by 12. A department Budget already accepted for
    /// that month is skipped, so sending twice records nothing twice; a refused one is sent again. Departments with
    /// no salary cost are left out.
    /// </summary>
    /// <exception cref="ValidationFailedException">Bad or future period (422).</exception>
    /// <exception cref="BudgetServiceException">Budget failed; departments sent before the failure stay recorded (502).</exception>
    public async Task<PayrollStatementsDto> SendStatementsAsync(SendPayrollStatementsRequest request)
    {
        var month = ParsePeriod(request.Period);
        if (string.CompareOrdinal(month, _clock.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture)) > 0)
            throw new ValidationFailedException("period", "A payroll statement cannot be sent for a month that has not started.");

        var asOf = AsOf(month);
        var payroll = await _payrollService.HeadcountAsync(asOf);
        var departments = (await _departmentRepository.ListWithServicesAsync()).ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);
        var existing = (await _statementRepository.ListForPeriodAsync(month)).ToDictionary(s => s.DepartmentId);

        foreach (var row in payroll.Departments.Where(r => r.TotalAnnualSalary > 0))
        {
            var department = departments[row.Code];
            if (existing.TryGetValue(department.DepartmentId, out var sent) && sent.Accepted) continue;

            var amount = Math.Round(row.TotalAnnualSalary / 12m, 2);
            var result = await _budget.CreateExpenseAsync(_budget.ToBudgetCode(department.Code), amount,
                $"Staff/HR payroll {month}: salary cost of {row.Headcount} staff ({department.Code})");

            // Only the key: the department list is read without tracking, and a navigation would be inserted as new.
            var statement = sent ?? new PayrollStatement { Period = month, DepartmentId = department.DepartmentId };
            statement.Headcount = row.Headcount;
            statement.Amount = amount;
            statement.Accepted = result.Approved;
            statement.Message = result.Message is { Length: > 500 } m ? m[..500] : result.Message;
            statement.ExpenseId = result.ExpenseId;
            statement.RemainingBudget = result.RemainingBudget;
            statement.SentAtUtc = _clock.UtcNow;
            statement.SentBy = _currentUser.ActorId;
            if (sent == null)
            {
                _statementRepository.Add(statement);
                existing[department.DepartmentId] = statement;
            }
            // Save each department as soon as Budget has it, so a later failure can't lose the record.
            await _statementRepository.SaveChangesAsync();
        }

        var codes = departments.Values.ToDictionary(d => d.DepartmentId, d => d.Code);
        return ToDto(month, asOf, existing.Values, codes);
    }

    private string ParsePeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period)) return _clock.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var trimmed = period.Trim();
        if (!PeriodPattern().IsMatch(trimmed))
            throw new ValidationFailedException("period", "Period must be a month written yyyy-MM, e.g. 2026-11.");
        return trimmed;
    }

    /// <summary>The month's last day, or today while the month is still running.</summary>
    private DateOnly AsOf(string month)
    {
        var first = DateOnly.ParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var last = first.AddMonths(1).AddDays(-1);
        return last < _clock.Today ? last : _clock.Today;
    }

    private PayrollStatementsDto ToDto(string month, DateOnly asOf, IEnumerable<PayrollStatement> statements,
        IReadOnlyDictionary<int, string> departmentCodes)
    {
        var rows = statements
            .Select(s => (Statement: s, Code: departmentCodes[s.DepartmentId]))
            .OrderBy(x => x.Code)
            .Select(x => (s: x.Statement, x.Code))
            .Select(x => new PayrollStatementDto(x.s.Period, x.Code, _budget.ToBudgetCode(x.Code), x.s.Headcount,
                x.s.Amount, x.s.Accepted, x.s.Message, x.s.ExpenseId, x.s.RemainingBudget, x.s.SentAtUtc, x.s.SentBy))
            .ToList();
        return new PayrollStatementsDto(month, asOf, rows, rows.Sum(r => r.Amount));
    }
}
