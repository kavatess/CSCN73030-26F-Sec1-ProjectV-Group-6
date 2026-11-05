using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>
/// Turns exceptions into RFC 7807 ProblemDetails. ApiException subclasses carry their own status;
/// a unique-index violation that slipped past a service check becomes 409; anything else is a 500.
/// </summary>
public class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem = exception switch
        {
            ValidationFailedException v => new ValidationProblemDetails(v.Errors)
            {
                Status = v.StatusCode, Title = v.Title, Detail = v.Message
            },
            ApiException api => new ProblemDetails { Status = api.StatusCode, Title = api.Title, Detail = api.Message },
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "The change conflicts with an existing record (a unique value is already in use)."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unexpected error",
                Detail = "An unexpected error occurred. Quote the traceId when reporting it."
            }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
