using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UpdateHistoryEntry;

public sealed class UpdateHistoryEntryHandler(LogbookOperations operations)
{
    public Task<ErrorOr<HistoryEntryResponse>> Handle(UpdateHistoryEntryCommand message, CancellationToken ct) => operations.UpdateHistoryEntryAsync(message, ct);
}
