using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.RemoveLink;

public sealed class RemoveLinkHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<Deleted>> Handle(RemoveLinkCommand message, CancellationToken ct) => operations.RemoveLinkAsync(message, ct);
}
