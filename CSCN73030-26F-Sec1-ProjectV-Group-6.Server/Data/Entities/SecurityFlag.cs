using System.ComponentModel.DataAnnotations;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A flag raised by Security against a person (for example a suspension).</summary>
public class SecurityFlag
{
    [Key]
    public int FlagId { get; set; }
    public int StaffId { get; set; }
    public string FlagType { get; set; } = string.Empty;
    public SecurityFlagStatus Status { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Staff Staff { get; set; } = null!;
}
