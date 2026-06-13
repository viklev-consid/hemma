using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.GetIssue;

public sealed class GetIssueHandler(IssuesOperations operations)
{
    public Task<ErrorOr<IssueResponse>> Handle(GetIssueQuery message, CancellationToken ct) => operations.GetIssueAsync(message, ct);
}
