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
    private static readonly string[] OperationalPathPrefixes =
    [
        "/admin/jobs"
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
            .Where(path => OperationalPathPrefixes.Any(prefix =>
                path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        foreach (var path in operationalPaths)
        {
            document.Paths.Remove(path);
        }

        return Task.CompletedTask;
    }
}
