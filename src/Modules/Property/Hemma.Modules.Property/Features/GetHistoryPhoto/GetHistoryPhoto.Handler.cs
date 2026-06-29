using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.GetHistoryPhoto;

public sealed class GetHistoryPhotoHandler(LogbookOperations operations)
{
    public Task<ErrorOr<HistoryPhotoContentResponse>> Handle(GetHistoryPhotoQuery message, CancellationToken ct) => operations.GetHistoryPhotoAsync(message, ct);
}
