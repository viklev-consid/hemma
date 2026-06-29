using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ReorderTasks;

public sealed class ReorderTasksHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<GetProjectTasksResponse>> Handle(ReorderTasksCommand message, CancellationToken ct) => operations.ReorderTasksAsync(message, ct);
}
