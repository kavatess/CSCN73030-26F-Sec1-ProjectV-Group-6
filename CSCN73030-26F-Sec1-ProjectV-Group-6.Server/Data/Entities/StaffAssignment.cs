using System.ComponentModel.DataAnnotations;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>Which service a person works in, and for how long. EndDate is null while current.</summary>
public class StaffAssignment
{
    [Key]
    public int AssignmentId { get; set; }
    public int StaffId { get; set; }
    public int ServiceId { get; set; }
    public string RoleTitle { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Staff Staff { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
