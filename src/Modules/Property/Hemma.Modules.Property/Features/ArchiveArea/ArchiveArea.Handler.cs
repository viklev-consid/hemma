using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ArchiveArea;

public sealed class ArchiveAreaHandler(AreasTagsOperations operations)
{
    public Task<ErrorOr<PropertyAreaResponse>> Handle(ArchiveAreaCommand message, CancellationToken ct) => operations.ArchiveAreaAsync(message, ct);
}
