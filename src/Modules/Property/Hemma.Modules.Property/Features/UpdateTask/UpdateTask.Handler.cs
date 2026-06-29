using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UpdateTask;

public sealed class UpdateTaskHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ProjectTaskResponse>> Handle(UpdateTaskCommand message, CancellationToken ct) => operations.UpdateTaskAsync(message, ct);
}
