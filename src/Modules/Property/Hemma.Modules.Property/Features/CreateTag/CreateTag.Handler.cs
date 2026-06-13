using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.CreateTag;

public sealed class CreateTagHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<PropertyTagResponse>> Handle(CreateTagCommand message, CancellationToken ct) => operations.CreateTagAsync(message, ct);
}
