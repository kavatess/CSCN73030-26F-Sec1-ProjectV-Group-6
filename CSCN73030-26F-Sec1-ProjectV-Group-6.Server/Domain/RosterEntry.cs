namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>One shift on the duty roster. Times are UTC; a shift covers [ShiftStart, ShiftEnd).</summary>
public class RosterEntry
{
    public int RosterEntryId { get; set; }
    public int StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public DateTime ShiftStart { get; set; }
    public DateTime ShiftEnd { get; set; }
    public ShiftType ShiftType { get; set; }
    public RosterStatus Status { get; set; }
}
