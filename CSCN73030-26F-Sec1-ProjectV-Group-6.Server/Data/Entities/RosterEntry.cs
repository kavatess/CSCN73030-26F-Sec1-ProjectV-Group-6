namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>One shift on the duty roster.</summary>
public class RosterEntry
{
    public int RosterEntryId { get; set; }
    public int StaffId { get; set; }
    public int ServiceId { get; set; }
    public DateTime ShiftStart { get; set; }
    public DateTime ShiftEnd { get; set; }

    public Staff Staff { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
