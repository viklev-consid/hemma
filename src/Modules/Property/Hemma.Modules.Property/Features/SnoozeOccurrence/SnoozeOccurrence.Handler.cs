using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.SnoozeOccurrence;

public sealed class SnoozeOccurrenceHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<MaintenanceOccurrenceResponse>> Handle(SnoozeOccurrenceCommand message, CancellationToken ct) =>
        operations.SnoozeOccurrenceAsync(message, ct);
}
