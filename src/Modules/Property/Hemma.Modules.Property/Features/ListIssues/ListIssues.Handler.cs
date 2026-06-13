using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ListIssues;

public sealed class ListIssuesHandler(IssuesOperations operations)
{
    public Task<ErrorOr<ListIssuesResponse>> Handle(ListIssuesQuery message, CancellationToken ct) => operations.ListIssuesAsync(message, ct);
}
