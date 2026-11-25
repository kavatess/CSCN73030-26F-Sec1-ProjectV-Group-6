/**
 * @file PayrollStatement.cs
 * @brief PayrollStatement entity: one month's salary cost of one department, sent to Budget as an expense.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>
/// A monthly salary cost statement sent to Budget and Treasury (POST /api/budget/v1/expenses). One per
/// department and month: a statement Budget accepted is never sent again; a refused one may be resent.
/// </summary>
public class PayrollStatement
{
    /// <summary>Surrogate key.</summary>
    public int PayrollStatementId { get; set; }

    /// <summary>Month the cost is for, "yyyy-MM".</summary>
    public string Period { get; set; } = "";

    /// <summary>Department the cost belongs to.</summary>
    public int DepartmentId { get; set; }

    /// <summary>The department.</summary>
    public Department Department { get; set; } = null!;

    /// <summary>People on payroll that month.</summary>
    public int Headcount { get; set; }

    /// <summary>Salary cost for the month (annual salaries / 12), CAD.</summary>
    public decimal Amount { get; set; }

    /// <summary>Whether Budget recorded the expense.</summary>
    public bool Accepted { get; set; }

    /// <summary>Budget's message, e.g. why it refused.</summary>
    public string? Message { get; set; }

    /// <summary>Budget's id for the expense, when recorded.</summary>
    public Guid? ExpenseId { get; set; }

    /// <summary>The department's remaining budget after the expense, as Budget reported it.</summary>
    public decimal? RemainingBudget { get; set; }

    /// <summary>When it was (last) sent, UTC.</summary>
    public DateTime SentAtUtc { get; set; }

    /// <summary>Who sent it.</summary>
    public string SentBy { get; set; } = "";
}
