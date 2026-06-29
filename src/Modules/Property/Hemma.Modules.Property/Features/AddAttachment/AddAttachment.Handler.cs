using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.AddAttachment;

public sealed class AddAttachmentHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ProjectAttachmentResponse>> Handle(AddAttachmentCommand message, CancellationToken ct) => operations.AddAttachmentAsync(message, ct);
}
