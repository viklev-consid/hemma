using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.GetProjectTasks;

public sealed class GetProjectTasksHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<GetProjectTasksResponse>> Handle(GetProjectTasksQuery message, CancellationToken ct) => operations.GetProjectTasksAsync(message, ct);
}
