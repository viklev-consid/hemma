using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ClearOccurrenceSnooze;

public sealed class ClearOccurrenceSnoozeHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<MaintenanceOccurrenceResponse>> Handle(ClearOccurrenceSnoozeCommand message, CancellationToken ct) =>
        operations.ClearOccurrenceSnoozeAsync(message, ct);
}
