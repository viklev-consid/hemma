using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ReorderAreas;

public sealed class ReorderAreasHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<ListAreasResponse>> Handle(ReorderAreasCommand message, CancellationToken ct) => operations.ReorderAreasAsync(message, ct);
}
