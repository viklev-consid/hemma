using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.PromoteOccurrenceToProject;

public sealed class PromoteOccurrenceToProjectHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<PromoteOccurrenceResponse>> Handle(PromoteOccurrenceToProjectCommand message, CancellationToken ct) => operations.PromoteOccurrenceToProjectAsync(message, ct);
}
