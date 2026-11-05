using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Infrastructure;

/// <summary>Sets the title and description at the top of the OpenAPI document.</summary>
public class ApiDocumentTransformer : IOpenApiDocumentTransformer
{
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
                "All roster times are UTC. Salary data is never exposed by this API."
        };
        return Task.CompletedTask;
    }
}
