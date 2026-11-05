namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A flag synced from the Security service. Schema only in this release.</summary>
public class SecurityFlag
{
    public int FlagId { get; set; }
    public int StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
    public string FlagType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}
