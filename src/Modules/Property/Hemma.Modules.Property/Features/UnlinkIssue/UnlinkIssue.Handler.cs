using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UnlinkIssue;

public sealed class UnlinkIssueHandler(IssuesOperations operations)
{
    public Task<ErrorOr<IssueResponse>> Handle(UnlinkIssueCommand message, CancellationToken ct) => operations.UnlinkIssueAsync(message, ct);
}
