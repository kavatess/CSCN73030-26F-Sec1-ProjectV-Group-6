/**
 * @file BudgetDtos.cs
 * @brief Request and response shapes for the Budget and Treasury integration (department budgets, payroll statements).
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Contracts;

/// <summary>A department's budget, as Budget and Treasury reports it.</summary>
/// <param name="Code">Our department code, e.g. PTR.</param>
/// <param name="Name">Our department name.</param>
/// <param name="BudgetDepartmentCode">The code Budget uses, e.g. TRANSIT.</param>
/// <param name="Allocated">Budget allocated for the year, CAD.</param>
/// <param name="Spent">Spent so far, CAD.</param>
/// <param name="Remaining">Left to spend, CAD.</param>
/// <param name="LowBudget">Budget's warning that little is left.</param>
public record DepartmentBudgetDto(string Code, string Name, string BudgetDepartmentCode, decimal Allocated, decimal Spent,
    decimal Remaining, bool LowBudget);

/// <summary>Body of POST /headcounts/payroll/statements.</summary>
/// <param name="Period">Month to send, "yyyy-MM". Defaults to the current month.</param>
public record SendPayrollStatementsRequest(string? Period);

/// <summary>One department's salary cost for a month, as sent to Budget.</summary>
/// <param name="Period">Month, "yyyy-MM".</param>
/// <param name="DepartmentCode">Our department code.</param>
/// <param name="BudgetDepartmentCode">The code Budget recorded it under.</param>
/// <param name="Headcount">People on payroll.</param>
/// <param name="Amount">Salary cost for the month (annual salaries / 12), CAD.</param>
/// <param name="Accepted">Whether Budget recorded the expense.</param>
/// <param name="Message">Budget's message, e.g. why it refused.</param>
/// <param name="ExpenseId">Budget's expense id, when recorded.</param>
/// <param name="RemainingBudget">The department's budget left afterwards, per Budget.</param>
/// <param name="SentAtUtc">When it was sent, UTC.</param>
/// <param name="SentBy">Who sent it.</param>
public record PayrollStatementDto(string Period, string DepartmentCode, string BudgetDepartmentCode, int Headcount, decimal Amount,
    bool Accepted, string? Message, Guid? ExpenseId, decimal? RemainingBudget, DateTime SentAtUtc, string SentBy);

/// <summary>The payroll statements of one month.</summary>
/// <param name="Period">Month, "yyyy-MM".</param>
/// <param name="AsOf">Payroll date the amounts were taken from.</param>
/// <param name="Statements">One per department with salary cost.</param>
/// <param name="TotalAmount">Sum of all amounts, CAD.</param>
public record PayrollStatementsDto(string Period, DateOnly AsOf, IReadOnlyList<PayrollStatementDto> Statements, decimal TotalAmount);
