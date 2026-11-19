/**
 * @file ProblemResponseAttribute.cs
 * @brief Attribute that declares an error response and its Swagger example.
 */

using Microsoft.AspNetCore.Mvc;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>
/// Declares an RFC 7807 error response for an endpoint and the example 'detail' shown in Swagger.
/// 400 and 422 use ValidationProblemDetails (with an 'errors' dictionary); others use ProblemDetails.
/// </summary>
/// <example>[ProblemResponse(StatusCodes.Status404NotFound, "Staff 'STF-0000042' was not found.")]</example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class ProblemResponseAttribute : ProducesResponseTypeAttribute
{
    /// <summary>Declares the error response.</summary>
    /// <param name="statusCode">400, 404, 409 or 422.</param>
    /// <param name="exampleDetail">Example 'detail' text for Swagger.</param>
    public ProblemResponseAttribute(int statusCode, string exampleDetail)
        : base(SchemaFor(statusCode), statusCode, "application/problem+json")
    {
        ExampleDetail = exampleDetail;
    }

    /// <summary>Example 'detail' text shown in Swagger.</summary>
    public string ExampleDetail { get; }

    /// <summary>Optional example for the 'errors' dictionary, as "field=message" pairs.</summary>
    public string[] ExampleErrors { get; init; } = [];

    private static Type SchemaFor(int statusCode) =>
        statusCode is StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity
            ? typeof(ValidationProblemDetails)
            : typeof(ProblemDetails);
}
