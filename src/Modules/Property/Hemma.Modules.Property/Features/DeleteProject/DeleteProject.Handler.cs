using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.DeleteProject;

public sealed class DeleteProjectHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<Deleted>> Handle(DeleteProjectCommand message, CancellationToken ct) => operations.DeleteProjectAsync(message, ct);
}
