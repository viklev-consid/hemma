using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ChangeProjectStatus;

public sealed class ChangeProjectStatusHandler(ProjectsOperations operations)
{
    public Task<ErrorOr<ChangeProjectStatusResponse>> Handle(ChangeProjectStatusCommand message, CancellationToken ct) => operations.ChangeProjectStatusAsync(message, ct);
}
