using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ArchiveTag;

public sealed class ArchiveTagHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<PropertyTagResponse>> Handle(ArchiveTagCommand message, CancellationToken ct) => operations.ArchiveTagAsync(message, ct);
}
