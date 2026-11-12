/**
 * @file AuthOptions.cs
 * @brief Configuration for authentication (section "Authentication").
 */

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;

/// <summary>Settings from the "Authentication" configuration section.</summary>
public class AuthOptions
{
    /// <summary>Configuration section name.</summary>
    public const string Section = "Authentication";

    /// <summary>"EntraId" (default: validate Entra ID access tokens) or "Development" (Development environment only).</summary>
    public string Mode { get; set; } = "EntraId";

    /// <summary>Entra ID settings, used when Mode is EntraId.</summary>
    public EntraIdSettings EntraId { get; set; } = new();

    /// <summary>True when Mode is Development.</summary>
    public bool IsDevelopmentMode => string.Equals(Mode, "Development", StringComparison.OrdinalIgnoreCase);

    /// <summary>Entra ID (Azure AD) app registration settings.</summary>
    public class EntraIdSettings
    {
        /// <summary>Sign-in authority host.</summary>
        public string Instance { get; set; } = "https://login.microsoftonline.com/";

        /// <summary>Directory (tenant) id of the college's Entra ID tenant.</summary>
        public string TenantId { get; set; } = "";

        /// <summary>Application ID URI of the Staff/HR API, e.g. api://cmes-staff-hr.</summary>
        public string Audience { get; set; } = "";

        /// <summary>Application (client) id of the Staff/HR API; accepted as an audience too (v2 tokens).</summary>
        public string ClientId { get; set; } = "";
    }
}
