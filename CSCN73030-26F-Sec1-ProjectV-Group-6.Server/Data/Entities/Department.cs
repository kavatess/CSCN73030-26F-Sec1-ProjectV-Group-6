namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A city department (Staff/HR, Security, Budget, ...).</summary>
public class Department
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public List<Service> Services { get; set; } = new();
}
