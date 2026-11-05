namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

/// <summary>A kind of licence or certification, e.g. Primary Care Paramedic Licence.</summary>
public class CredentialType
{
    public int CredentialTypeId { get; set; }
    public string Name { get; set; } = "";

    /// <summary>When true, an expired or suspended credential of this type stops the holder working shifts.</summary>
    public bool IsDutyBlocking { get; set; }
}
