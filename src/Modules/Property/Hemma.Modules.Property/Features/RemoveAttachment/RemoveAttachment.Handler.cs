using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.RemoveAttachment;

public sealed class RemoveAttachmentHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<Deleted>> Handle(RemoveAttachmentCommand message, CancellationToken ct) => operations.RemoveAttachmentAsync(message, ct);
}
