namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A city department, e.g. Emergency Services (code EMS).</summary>
public class Department
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Short public code (EMS, SEC, PTR, MNT, BUD, RES, HRS). Used in URLs.</summary>
    public string Code { get; set; } = "";

    public List<Service> Services { get; set; } = [];
}
