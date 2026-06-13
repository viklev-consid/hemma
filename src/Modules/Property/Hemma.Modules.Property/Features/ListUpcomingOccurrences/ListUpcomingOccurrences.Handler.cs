using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ListUpcomingOccurrences;

public sealed class ListUpcomingOccurrencesHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<ListUpcomingOccurrencesResponse>> Handle(ListUpcomingOccurrencesQuery message, CancellationToken ct) => operations.ListUpcomingOccurrencesAsync(message, ct);
}
