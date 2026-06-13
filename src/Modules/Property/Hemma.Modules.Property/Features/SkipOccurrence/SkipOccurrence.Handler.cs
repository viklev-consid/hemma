using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.SkipOccurrence;

public sealed class SkipOccurrenceHandler(MaintenanceOperations operations)
{
    public Task<ErrorOr<SkipOccurrenceResponse>> Handle(SkipOccurrenceCommand message, CancellationToken ct) => operations.SkipOccurrenceAsync(message, ct);
}
