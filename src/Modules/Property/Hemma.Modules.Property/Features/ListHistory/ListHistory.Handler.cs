using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ListHistory;

public sealed class ListHistoryHandler(LogbookOperations operations)
{
    public Task<ErrorOr<ListHistoryResponse>> Handle(ListHistoryQuery message, CancellationToken ct) => operations.ListHistoryAsync(message, ct);
}
