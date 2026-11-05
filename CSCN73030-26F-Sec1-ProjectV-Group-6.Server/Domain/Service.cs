namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>An operational service inside a department, e.g. the Paramedic Service in EMS.</summary>
public class Service
{
    public int ServiceId { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Name { get; set; } = "";
}
