/**
 * @file StaffAssignment.cs
 * @brief StaffAssignment entity: a role held in a service for a period.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A role a staff member holds in a service for a period. EndDate null = open-ended.</summary>
/// @par Business rule 6
/// StartDate must be before EndDate (validated in AssignmentService and by a check constraint).
public class StaffAssignment
{
    /// <summary>Database key, also the id used by <c>PATCH /assignments/{id}</c>.</summary>
    public int AssignmentId { get; set; }

    /// <summary>Assigned staff member's key.</summary>
    public int StaffId { get; set; }

    /// <summary>Assigned staff member.</summary>
    public Staff Staff { get; set; } = null!;

    /// <summary>Service the role is in.</summary>
    public int ServiceId { get; set; }

    /// <summary>Service the role is in.</summary>
    public Service Service { get; set; } = null!;

    /// <summary>Job title in this assignment, e.g. "Primary Care Paramedic".</summary>
    public string RoleTitle { get; set; } = "";

    /// <summary>First day of the assignment.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Last day of the assignment, or null while it is open-ended.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>True when the assignment covers the given day (inclusive at both ends).</summary>
    /// <param name="day">The day to check, normally today (UTC).</param>
    public bool IsCurrentOn(DateOnly day) => StartDate <= day && (EndDate == null || EndDate >= day);
}
