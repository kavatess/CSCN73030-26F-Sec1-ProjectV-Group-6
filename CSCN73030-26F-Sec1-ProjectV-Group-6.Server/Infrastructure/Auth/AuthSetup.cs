/**
 * @file AuthSetup.cs
 * @brief Registers authentication (Entra ID JWT or development) and the role-based policies.
 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;

/// <summary>Authentication and authorization wiring for Program.cs.</summary>
public static class AuthSetup
{
    /// <summary>
    /// EntraId mode validates Entra ID access tokens (issuer, audience, signature, lifetime) and reads
    /// app roles from the "roles" claim. Development mode signs everyone in as dev-user and is refused
    /// outside the Development environment.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <exception cref="InvalidOperationException">Development mode outside the Development environment.</exception>
    public static void AddStaffHrAuth(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(AuthOptions.Section).Get<AuthOptions>() ?? new AuthOptions();

        if (options.IsDevelopmentMode)
        {
            if (!builder.Environment.IsDevelopment())
                throw new InvalidOperationException(
                    "Authentication:Mode 'Development' signs every request in without a token and is only allowed in the Development environment.");

            builder.Services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthHandler>(DevelopmentAuthHandler.SchemeName, _ => { });
        }
        else
        {
            var entra = options.EntraId;
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwt =>
                {
                    if (!string.IsNullOrWhiteSpace(entra.TenantId))
                        jwt.Authority = $"{entra.Instance.TrimEnd('/')}/{entra.TenantId}/v2.0";
                    jwt.MapInboundClaims = false;
                    jwt.TokenValidationParameters.ValidAudiences =
                        new[] { entra.Audience, entra.ClientId }.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
                    // Entra ID issues v1 (sts.windows.net) or v2 (login.microsoftonline.com) tokens.
                    if (!string.IsNullOrWhiteSpace(entra.TenantId))
                        jwt.TokenValidationParameters.ValidIssuers =
                        [
                            $"{entra.Instance.TrimEnd('/')}/{entra.TenantId}/v2.0",
                            $"https://sts.windows.net/{entra.TenantId}/",
                        ];
                    jwt.TokenValidationParameters.RoleClaimType = "roles";
                    jwt.TokenValidationParameters.NameClaimType = "preferred_username";
                });
        }

        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationHandler>();
        builder.Services.AddAuthorization(auth =>
        {
            auth.AddPolicy(StaffHrPolicies.Read, p => p.RequireRole(StaffHrRoles.All));
            auth.AddPolicy(StaffHrPolicies.Manage, p => p.RequireRole(StaffHrRoles.Admin));
            auth.AddPolicy(StaffHrPolicies.ManageRosters, p => p.RequireRole(StaffHrRoles.Admin, StaffHrRoles.RostersWrite));
            auth.AddPolicy(StaffHrPolicies.SubmitRequisitions, p => p.RequireRole(StaffHrRoles.Admin, StaffHrRoles.RequisitionsSubmit));
            auth.AddPolicy(StaffHrPolicies.SecurityFlags, p => p.RequireRole(StaffHrRoles.Admin, StaffHrRoles.SecurityFlagsWrite));
            auth.AddPolicy(StaffHrPolicies.Payroll, p => p.RequireRole(StaffHrRoles.Payroll));
            auth.AddPolicy(StaffHrPolicies.Audit, p => p.RequireRole(StaffHrRoles.Admin, StaffHrRoles.AuditRead));
        });
    }
}
