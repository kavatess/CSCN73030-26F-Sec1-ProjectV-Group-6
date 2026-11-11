namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A service run by a department (for example police or fire inside Security/Emergency).</summary>
public class Service
{
    public int ServiceId { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Department Department { get; set; } = null!;
}
