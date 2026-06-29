using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.DeactivatePlan;

public sealed class DeactivatePlanHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<MaintenancePlanResponse>> Handle(DeactivatePlanCommand message, CancellationToken ct) => operations.DeactivatePlanAsync(message, ct);
}
