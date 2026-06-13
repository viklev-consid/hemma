using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.CreateHistoryEntry;

public sealed class CreateHistoryEntryHandler(LogbookOperations operations)
{
    public Task<ErrorOr<HistoryEntryResponse>> Handle(CreateHistoryEntryCommand message, CancellationToken ct) => operations.CreateHistoryEntryAsync(message, ct);
}
