using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.CompleteOccurrence;

public sealed class CompleteOccurrenceHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<CompleteOccurrenceResponse>> Handle(CompleteOccurrenceCommand message, CancellationToken ct) => operations.CompleteOccurrenceAsync(message, ct);
}
