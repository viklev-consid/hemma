using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UpdateMaintenancePlan;

public sealed class UpdateMaintenancePlanHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<MaintenancePlanResponse>> Handle(UpdateMaintenancePlanCommand message, CancellationToken ct) => operations.UpdateMaintenancePlanAsync(message, ct);
}
