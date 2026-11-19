/**
 * @file ProblemDetailsResult.cs
 * @brief Action result that writes a ProblemDetails body as application/problem+json.
 */

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>Writes a ProblemDetails body (including any 'errors' member) as application/problem+json.</summary>
/// <remarks>
/// Used for the 400 responses produced by model binding. An ObjectResult would not do: the
/// controllers' <c>[Produces("application/json")]</c> filter overwrites an ObjectResult's content
/// type, so the error would go out as application/json instead of application/problem+json.
/// The body is serialised by its runtime type so a ValidationProblemDetails keeps its 'errors'.
/// </remarks>
public class ProblemDetailsResult : IActionResult
{
    private readonly ProblemDetails _problem;

    /// <summary>Creates the result.</summary>
    /// <param name="problem">The body to send; its Status sets the HTTP status code.</param>
    public ProblemDetailsResult(ProblemDetails problem)
    {
        _problem = problem;
    }

    /// <summary>Writes the response.</summary>
    /// <param name="context">The action context.</param>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        var json = context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        response.StatusCode = _problem.Status ?? StatusCodes.Status400BadRequest;
        response.ContentType = "application/problem+json; charset=utf-8";
        await JsonSerializer.SerializeAsync(response.Body, _problem, _problem.GetType(), json, context.HttpContext.RequestAborted);
    }
}
