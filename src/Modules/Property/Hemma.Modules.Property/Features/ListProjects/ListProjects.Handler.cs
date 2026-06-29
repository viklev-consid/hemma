using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ListProjects;

public sealed class ListProjectsHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ListProjectsResponse>> Handle(ListProjectsQuery message, CancellationToken ct) => operations.ListProjectsAsync(message, ct);
}
