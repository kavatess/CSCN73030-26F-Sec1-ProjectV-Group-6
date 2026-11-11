namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Data.Entities;

/// <summary>A kind of certification or licence (paramedic, heavy machinery, ...).</summary>
public class CredentialType
{
    public int CredentialTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
}
