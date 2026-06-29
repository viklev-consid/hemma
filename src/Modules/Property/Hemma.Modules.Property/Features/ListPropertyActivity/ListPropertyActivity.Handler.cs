using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ListPropertyActivity;

public sealed class ListPropertyActivityHandler(ActivityOperations operations)
{
    public Task<ErrorOr<ListPropertyActivityResponse>> Handle(ListPropertyActivityQuery query, CancellationToken ct) =>
        operations.ListPropertyActivityAsync(query, ct);
}
