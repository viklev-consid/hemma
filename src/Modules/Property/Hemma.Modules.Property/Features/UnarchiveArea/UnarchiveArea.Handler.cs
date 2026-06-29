using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UnarchiveArea;

public sealed class UnarchiveAreaHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<PropertyAreaResponse>> Handle(UnarchiveAreaCommand message, CancellationToken ct) => operations.UnarchiveAreaAsync(message, ct);
}
