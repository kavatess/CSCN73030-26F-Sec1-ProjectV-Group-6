/**
 * @file ProblemDetailsAuthorizationHandler.cs
 * @brief Writes 401 and 403 as RFC 7807 ProblemDetails, like every other error.
 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure.Auth;

/// <summary>Turns authorization failures into application/problem+json 401 / 403 responses.</summary>
public class ProblemDetailsAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    /// <summary>Writes the problem response, or lets the request through when authorized.</summary>
    /// <param name="next">The rest of the pipeline.</param>
    /// <param name="context">The request.</param>
    /// <param name="policy">The policy evaluated.</param>
    /// <param name="authorizeResult">Outcome of the evaluation.</param>
    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var forbidden = authorizeResult.Forbidden;
        if (!forbidden)
        {
            // Let the scheme add its WWW-Authenticate header, then write the body.
            foreach (var scheme in policy.AuthenticationSchemes.DefaultIfEmpty(null))
            {
                if (scheme == null) await context.ChallengeAsync();
                else await context.ChallengeAsync(scheme);
            }
        }

        context.Response.StatusCode = forbidden ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
        await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = forbidden
                ? new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden, Title = "Forbidden",
                    Detail = "Your token does not carry a role allowed to do this.",
                }
                : new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized, Title = "Unauthorized",
                    Detail = "Send a valid Entra ID access token for the Staff/HR API in the Authorization header.",
                },
        });
    }
}
