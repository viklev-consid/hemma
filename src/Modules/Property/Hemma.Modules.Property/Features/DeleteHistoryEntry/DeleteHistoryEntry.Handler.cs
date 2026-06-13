using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.DeleteHistoryEntry;

public sealed class DeleteHistoryEntryHandler(LogbookOperations operations)
{
    public Task<ErrorOr<Deleted>> Handle(DeleteHistoryEntryCommand message, CancellationToken ct) => operations.DeleteHistoryEntryAsync(message, ct);
}
