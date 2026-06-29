using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UpdateProject;

public sealed class UpdateProjectHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ProjectResponse>> Handle(UpdateProjectCommand message, CancellationToken ct) => operations.UpdateProjectAsync(message, ct);
}
