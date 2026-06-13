using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.CreateProject;

public sealed class CreateProjectHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ProjectResponse>> Handle(CreateProjectCommand message, CancellationToken ct) => operations.CreateProjectAsync(message, ct);
}
