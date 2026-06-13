using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.AssignTags;

public sealed class AssignTagsHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<AssignTagsResponse>> Handle(AssignTagsCommand message, CancellationToken ct) => operations.AssignTagsAsync(message, ct);
}
