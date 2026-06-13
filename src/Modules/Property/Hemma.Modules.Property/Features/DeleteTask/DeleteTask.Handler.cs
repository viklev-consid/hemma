using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.DeleteTask;

public sealed class DeleteTaskHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<Deleted>> Handle(DeleteTaskCommand message, CancellationToken ct) => operations.DeleteTaskAsync(message, ct);
}
