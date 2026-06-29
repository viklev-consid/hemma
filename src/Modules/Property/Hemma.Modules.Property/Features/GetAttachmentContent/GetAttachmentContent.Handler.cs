using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.GetAttachmentContent;

public sealed class GetAttachmentContentHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<AttachmentContentResponse>> Handle(GetAttachmentContentQuery message, CancellationToken ct) => operations.GetAttachmentContentAsync(message, ct);
}
