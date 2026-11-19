/**
 * @file RequestSchemaTransformer.cs
 * @brief Fixes the required-field list of request schemas in the OpenAPI document.
 */

using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>
/// The OpenAPI generator marks every positional record parameter as required. For request
/// bodies that is wrong (PATCH bodies are all optional), so replace the list with the fields
/// named in <see cref="RequiredFieldsAttribute"/>, or none.
/// </summary>
public class RequestSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <summary>Replaces the required list of a *Request schema with its <see cref="RequiredFieldsAttribute"/> fields.</summary>
    /// <param name="schema">The schema being generated.</param>
    /// <param name="context">Generator context.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken ct)
    {
        var type = context.JsonTypeInfo.Type;
        if (type.Name.EndsWith("Request", StringComparison.Ordinal))
        {
            var fields = type.GetCustomAttribute<RequiredFieldsAttribute>()?.Fields ?? [];
            schema.Required = new HashSet<string>(fields);
        }
        return Task.CompletedTask;
    }
}
