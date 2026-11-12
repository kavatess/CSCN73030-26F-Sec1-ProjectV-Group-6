/**
 * @file ApiDocumentTransformer.cs
 * @brief Sets the OpenAPI document title and description.
 */

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>Sets the title and description at the top of the OpenAPI document.</summary>
public class ApiDocumentTransformer : IOpenApiDocumentTransformer
{
    /// <summary>Replaces the document info block.</summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="context">Generator context.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info = new OpenApiInfo
        {
            Title = "CMES Staff/HR API",
            Version = "v1",
            Description =
                "Single source of truth for City Management Enterprise System employees: departments, services, " +
                "staff, assignments, duty rosters, hire requisitions and credentials.\n\n" +
                "Errors use RFC 7807 ProblemDetails (`application/problem+json`): 400 malformed request, " +
                "404 not found, 409 conflict, 422 validation failure with a field-level `errors` dictionary. " +
                "All roster times are UTC. Salary is returned only by the payroll endpoints (StaffHr.Payroll role).\n\n" +
                "Authentication: Entra ID bearer tokens carrying app roles (see the Bearer security scheme). " +
                "401 = no or invalid token, 403 = the token's roles do not allow the request."
        };
        // Entra ID bearer tokens; app roles are listed in the description.
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Entra ID access token for the Staff/HR API. App roles: StaffHr.Admin, StaffHr.Read, " +
                          "StaffHr.Rosters.Write, StaffHr.Requisitions.Submit, StaffHr.SecurityFlags.Write, " +
                          "StaffHr.Payroll, StaffHr.Audit.Read. In Development no token is needed.",
        };
        return Task.CompletedTask;
    }
}
