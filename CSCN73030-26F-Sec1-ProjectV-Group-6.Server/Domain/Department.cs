/**
 * @file Department.cs
 * @brief Department entity.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A city department, e.g. Emergency Services (code EMS).</summary>
public class Department
{
    /// <summary>Database key. Internal; the API identifies departments by <see cref="Code"/>.</summary>
    public int DepartmentId { get; set; }

    /// <summary>Display name, e.g. "Emergency Services".</summary>
    public string Name { get; set; } = "";

    /// <summary>Short public code (EMS, SEC, PTR, MNT, BUD, RES, HRS). Unique; used in URLs.</summary>
    public string Code { get; set; } = "";

    /// <summary>The operational services inside this department.</summary>
    public List<Service> Services { get; set; } = [];
}
