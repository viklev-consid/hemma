using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UpdateArea;

public sealed class UpdateAreaHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<PropertyAreaResponse>> Handle(UpdateAreaCommand message, CancellationToken ct) => operations.UpdateAreaAsync(message, ct);
}
