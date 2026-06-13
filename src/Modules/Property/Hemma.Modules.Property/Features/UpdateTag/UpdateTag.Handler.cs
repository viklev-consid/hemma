using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UpdateTag;

public sealed class UpdateTagHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<PropertyTagResponse>> Handle(UpdateTagCommand message, CancellationToken ct) => operations.UpdateTagAsync(message, ct);
}
