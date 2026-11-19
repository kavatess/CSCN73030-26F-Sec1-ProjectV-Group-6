/**
 * @file DevelopmentAuthHandler.cs
 * @brief Development-only sign-in that needs no tokens.
 */

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;

/// <summary>
/// Signs every request in as "dev-user" with every role, so Swagger and the React client work locally
/// without Entra ID. Refused outside the Development environment (see AuthSetup).
/// Headers let tests and developers act as someone else:
/// <list type="bullet">
/// <item><c>X-Dev-Anonymous: true</c> — not signed in (401 on protected endpoints);</item>
/// <item><c>X-Dev-User: name</c> — the user name (audit ActorId);</item>
/// <item><c>X-Dev-Roles: StaffHr.Read,StaffHr.Payroll</c> — only these roles (<c>none</c> = signed in with no roles);</item>
/// <item><c>X-Dev-Department: EMS</c> — a department claim, which limits what the caller sees.</item>
/// </list>
/// </summary>
public class DevelopmentAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Authentication scheme name.</summary>
    public const string SchemeName = "Development";

    /// <summary>Creates the handler.</summary>
    /// <param name="options">Scheme options.</param>
    /// <param name="logger">Logger factory.</param>
    /// <param name="encoder">URL encoder.</param>
    public DevelopmentAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>Builds the development identity from the request headers.</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers;
        if (string.Equals(headers["X-Dev-Anonymous"], "true", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var user = headers["X-Dev-User"].FirstOrDefault() ?? "dev-user";
        var roles = headers.ContainsKey("X-Dev-Roles")
            ? headers["X-Dev-Roles"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(r => !string.Equals(r, "none", StringComparison.OrdinalIgnoreCase)).ToArray()
            : StaffHrRoles.All;

        var claims = new List<Claim> { new("preferred_username", user), new(ClaimTypes.Name, user) };
        claims.AddRange(roles.Select(r => new Claim("roles", r)));
        var department = headers["X-Dev-Department"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(department)) claims.Add(new Claim(StaffHrClaims.Department, department));

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, "roles");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

/// <summary>Custom claim names.</summary>
public static class StaffHrClaims
{
    /// <summary>Department code a caller is limited to (e.g. a peer service acting for one department).</summary>
    public const string Department = "department";
}
