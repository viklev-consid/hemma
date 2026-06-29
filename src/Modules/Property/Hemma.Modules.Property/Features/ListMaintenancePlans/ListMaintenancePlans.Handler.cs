using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ListMaintenancePlans;

public sealed class ListMaintenancePlansHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<ListMaintenancePlansResponse>> Handle(ListMaintenancePlansQuery message, CancellationToken ct) => operations.ListMaintenancePlansAsync(message, ct);
}
