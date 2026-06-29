using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.GetProject;

public sealed class GetProjectHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ProjectResponse>> Handle(GetProjectQuery message, CancellationToken ct) => operations.GetProjectAsync(message, ct);
}
