using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Hemma.Api.Infrastructure.OpenApi;

/// <summary>
/// Keeps the generated product contract focused on customer-facing API routes.
/// Operator tools such as the scheduler dashboard are hosted by the same process,
/// but frontend clients should not generate against them.
/// </summary>
internal sealed class ProductOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private static readonly string[] operationalPathPrefixes =
    [
        "/admin/jobs",
        "/api/",
        "/v1/admin/"
    ];

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Paths is null)
        {
            return Task.CompletedTask;
        }

        var operationalPaths = document.Paths
            .Keys
            .Where(path => operationalPathPrefixes.Any(prefix =>
                path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        foreach (var path in operationalPaths)
        {
            document.Paths.Remove(path);
        }

        if (document.Tags is not null)
        {
            document.Tags = document.Tags
                .Where(tag => tag.Name is null ||
                    !tag.Name.Contains("Ticker", StringComparison.OrdinalIgnoreCase))
                .ToHashSet();
        }

        if (document.Components?.Schemas is not null)
        {
            var operationalSchemas = document.Components.Schemas
                .Keys
                .Where(name => name.Contains("Ticker", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var schemaName in operationalSchemas)
            {
                document.Components.Schemas.Remove(schemaName);
            }
        }

        return Task.CompletedTask;
    }
}
