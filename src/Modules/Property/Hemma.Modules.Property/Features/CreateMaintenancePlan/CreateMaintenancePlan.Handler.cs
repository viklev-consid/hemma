using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.CreateMaintenancePlan;

public sealed class CreateMaintenancePlanHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<GetMaintenancePlanResponse>> Handle(CreateMaintenancePlanCommand message, CancellationToken ct) => operations.CreateMaintenancePlanAsync(message, ct);
}
