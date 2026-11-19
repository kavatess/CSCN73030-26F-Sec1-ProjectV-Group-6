/**
 * @file Requisition.cs
 * @brief Requisition entity: a hire (later, termination) request from a department.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A hire (or, later, termination) request raised by a department.</summary>
public class Requisition
{
    /// <summary>Database key. Internal; the API uses <see cref="RequisitionNumber"/>.</summary>
    public int RequisitionId { get; set; }

    /// <summary>REQ-0000000, allocated from its own sequence. Unique.</summary>
    public string RequisitionNumber { get; set; } = "";

    /// <summary>Requesting department's key.</summary>
    public int RequestingDeptId { get; set; }

    /// <summary>Requesting department.</summary>
    public Department RequestingDepartment { get; set; } = null!;

    /// <summary>The employee concerned. Used by termination requisitions (not built yet).</summary>
    public int? StaffId { get; set; }

    /// <summary>The employee concerned, if any.</summary>
    public Staff? Staff { get; set; }

    /// <summary>Hire or Termination. Only Hire is created in this release.</summary>
    public RequisitionType Type { get; set; }

    /// <summary>Job title being hired for.</summary>
    public string RoleTitle { get; set; } = "";

    /// <summary>Number of people to hire (1 to 100).</summary>
    public int Positions { get; set; }

    /// <summary>How many of <see cref="Positions"/> are filled (0 to Positions).</summary>
    public int PositionsFilled { get; set; }

    /// <summary>Where the requisition is in its workflow (<see cref="RequisitionWorkflow"/>).</summary>
    public RequisitionStatus Status { get; set; }

    /// <summary>Department's reason, e.g. GROWTH or ATTRITION. Free text, optional.</summary>
    public string? ReasonCode { get; set; }

    /// <summary>Budget &amp; Treasury's decision, or null before Budget is asked.</summary>
    public BudgetDecision? BudgetDecision { get; set; }

    /// <summary>Client-supplied Idempotency-Key header value. Unique.</summary>
    /// @par Business rule 3
    /// Replaying a key returns the original requisition instead of creating a second one.
    public string IdempotencyKey { get; set; } = "";

    /// <summary>When the requisition was created, UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }
}
