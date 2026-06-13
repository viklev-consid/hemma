using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.GetPropertyActivitySummary;

public sealed class GetPropertyActivitySummaryHandler(ActivityOperations operations)
{
    public Task<ErrorOr<PropertyActivitySummaryResponse>> Handle(GetPropertyActivitySummaryQuery query, CancellationToken ct) =>
        operations.GetPropertyActivitySummaryAsync(query, ct);
}
