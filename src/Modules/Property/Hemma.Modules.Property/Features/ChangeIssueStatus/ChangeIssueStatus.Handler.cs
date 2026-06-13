using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ChangeIssueStatus;

public sealed class ChangeIssueStatusHandler(IssuesOperations operations)
{
    public Task<ErrorOr<IssueResponse>> Handle(ChangeIssueStatusCommand message, CancellationToken ct) => operations.ChangeIssueStatusAsync(message, ct);
}
