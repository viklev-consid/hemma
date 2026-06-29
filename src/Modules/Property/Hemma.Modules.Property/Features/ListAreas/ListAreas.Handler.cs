using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ListAreas;

public sealed class ListAreasHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<ListAreasResponse>> Handle(ListAreasQuery message, CancellationToken ct) => operations.ListAreasAsync(message, ct);
}
