using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>
/// Adds a description and a realistic RFC 7807 example body to every error response that an
/// endpoint declares with <see cref="ProblemResponseAttribute"/>, so reviewers from other groups
/// can see exactly what our errors look like in Swagger.
/// </summary>
public class ProblemExamplesTransformer : IOpenApiOperationTransformer
{
    private static readonly Dictionary<int, (string Title, string Type, string Description)> Known = new()
    {
        [400] = ("Malformed request", "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            "Malformed request: unreadable JSON, a value of the wrong type, or a missing required header."),
        [404] = ("Resource not found", "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            "The resource in the URL does not exist."),
        [409] = ("Conflict", "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            "The request conflicts with existing data."),
        [422] = ("Validation failed", "https://tools.ietf.org/html/rfc4918#section-11.2",
            "Validation failure. 'errors' maps each invalid field to its messages."),
    };

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        var declared = context.Description.ActionDescriptor.EndpointMetadata.OfType<ProblemResponseAttribute>();
        foreach (var attribute in declared)
        {
            var code = attribute.StatusCode.ToString();
            if (operation.Responses?.TryGetValue(code, out var response) != true || response is not OpenApiResponse concrete)
                continue;

            Known.TryGetValue(attribute.StatusCode, out var known);
            concrete.Description = known.Description ?? concrete.Description;

            if (concrete.Content?.TryGetValue("application/problem+json", out var media) == true)
            {
                media.Example = BuildExample(attribute, known.Title, known.Type);
                // Errors are only ever sent as problem+json; drop the controller-level application/json entry.
                concrete.Content = new Dictionary<string, OpenApiMediaType> { ["application/problem+json"] = media };
            }
        }

        // Business rule 3: POST /requisitions must carry an Idempotency-Key header.
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter is { In: ParameterLocation.Header, Name: "Idempotency-Key" })
            {
                parameter.Required = true;
                parameter.Description = "Unique value per new requisition (e.g. a GUID), max 100 characters. " +
                                        "Replaying a key with the same body returns the original requisition.";
            }
        }
        return Task.CompletedTask;
    }

    private static JsonObject BuildExample(ProblemResponseAttribute attribute, string? title, string? type)
    {
        var example = new JsonObject
        {
            ["type"] = type,
            ["title"] = title,
            ["status"] = attribute.StatusCode,
            ["detail"] = attribute.ExampleDetail,
        };

        if (attribute.StatusCode is 400 or 422)
        {
            var errors = new JsonObject();
            foreach (var pair in attribute.ExampleErrors)
            {
                var parts = pair.Split('=', 2);
                errors[parts[0]] = new JsonArray(parts.Length > 1 ? parts[1] : "Invalid value.");
            }
            example["errors"] = errors;
        }

        example["traceId"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00";
        return example;
    }
}
