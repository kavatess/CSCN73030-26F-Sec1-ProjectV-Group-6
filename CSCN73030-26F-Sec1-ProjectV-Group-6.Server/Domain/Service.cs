/**
 * @file Service.cs
 * @brief Service entity: an operational unit inside a department.
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>An operational service inside a department, e.g. the Paramedic Service in EMS.</summary>
public class Service
{
    /// <summary>Database key, also the public id clients filter by (<c>?service=</c>).</summary>
    public int ServiceId { get; set; }

    /// <summary>Owning department's key.</summary>
    public int DepartmentId { get; set; }

    /// <summary>Owning department.</summary>
    public Department Department { get; set; } = null!;

    /// <summary>Display name, unique within its department.</summary>
    public string Name { get; set; } = "";
}
