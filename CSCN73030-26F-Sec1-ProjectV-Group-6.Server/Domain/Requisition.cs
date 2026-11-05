namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A hire (or, later, termination) request raised by a department.</summary>
public class Requisition
{
    public int RequisitionId { get; set; }

    /// <summary>REQ-0000000, allocated from its own sequence.</summary>
    public string RequisitionNumber { get; set; } = "";

    public int RequestingDeptId { get; set; }
    public Department RequestingDepartment { get; set; } = null!;

    /// <summary>The employee concerned. Used by termination requisitions (not built yet).</summary>
    public int? StaffId { get; set; }
    public Staff? Staff { get; set; }

    public RequisitionType Type { get; set; }
    public string RoleTitle { get; set; } = "";
    public int Positions { get; set; }
    public int PositionsFilled { get; set; }
    public RequisitionStatus Status { get; set; }
    public string? ReasonCode { get; set; }
    public BudgetDecision? BudgetDecision { get; set; }

    /// <summary>Client-supplied Idempotency-Key header value. Unique.</summary>
    public string IdempotencyKey { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
}
