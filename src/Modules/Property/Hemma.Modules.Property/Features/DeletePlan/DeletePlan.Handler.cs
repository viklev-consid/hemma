using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.DeletePlan;

public sealed class DeletePlanHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<Deleted>> Handle(DeletePlanCommand message, CancellationToken ct) => operations.DeletePlanAsync(message, ct);
}
