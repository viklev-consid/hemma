using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UnarchiveTag;

public sealed class UnarchiveTagHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<PropertyTagResponse>> Handle(UnarchiveTagCommand message, CancellationToken ct) => operations.UnarchiveTagAsync(message, ct);
}
