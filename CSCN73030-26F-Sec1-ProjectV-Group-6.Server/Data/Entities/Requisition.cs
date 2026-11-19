namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A hire or termination request from a department. StaffId is empty for a hire until it is approved.</summary>
public class Requisition
{
    public int RequisitionId { get; set; }
    public int RequestingDeptId { get; set; }
    public int? StaffId { get; set; }
    public RequisitionType Type { get; set; }
    public RequisitionStatus Status { get; set; }
    public string? ReasonCode { get; set; }
    public string? RoleTitle { get; set; }
    public int? RequestedHeadcount { get; set; }
    public decimal? ProposedSalary { get; set; }
    public BudgetDecisionResult? BudgetDecision { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;

    public Department RequestingDepartment { get; set; } = null!;
    public Staff? Staff { get; set; }
}
