namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A role a staff member holds in a service for a period. EndDate null = open-ended.</summary>
public class StaffAssignment
{
    public int AssignmentId { get; set; }
    public int StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public string RoleTitle { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>True when the assignment covers the given day.</summary>
    public bool IsCurrentOn(DateOnly day) => StartDate <= day && (EndDate == null || EndDate >= day);
}
