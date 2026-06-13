using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.AddTask;

public sealed class AddTaskHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ProjectTaskResponse>> Handle(AddTaskCommand message, CancellationToken ct) => operations.AddTaskAsync(message, ct);
}
