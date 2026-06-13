using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.GetMaintenancePlan;

public sealed class GetMaintenancePlanHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<GetMaintenancePlanResponse>> Handle(GetMaintenancePlanQuery message, CancellationToken ct) => operations.GetMaintenancePlanAsync(message, ct);
}
