using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>
/// Declares an RFC 7807 error response for an endpoint and the example 'detail' shown in Swagger.
/// 400 and 422 use ValidationProblemDetails (with an 'errors' dictionary); others use ProblemDetails.
/// </summary>
/// <example>[ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000042' was not found.")]</example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class ProblemResponseAttribute(int statusCode, string exampleDetail)
    : ProducesResponseTypeAttribute(SchemaFor(statusCode), statusCode, "application/problem+json")
{
    public string ExampleDetail { get; } = exampleDetail;

    /// <summary>Optional example for the 'errors' dictionary, as "field=message" pairs.</summary>
    public string[] ExampleErrors { get; init; } = [];

    private static Type SchemaFor(int statusCode) =>
        statusCode is StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity
            ? typeof(ValidationProblemDetails)
            : typeof(ProblemDetails);
}
