using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.AddLink;

public sealed class AddLinkHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ProjectLinkResponse>> Handle(AddLinkCommand message, CancellationToken ct) => operations.AddLinkAsync(message, ct);
}
