using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ListTags;

public sealed class ListTagsHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<ListTagsResponse>> Handle(ListTagsQuery message, CancellationToken ct) => operations.ListTagsAsync(message, ct);
}
