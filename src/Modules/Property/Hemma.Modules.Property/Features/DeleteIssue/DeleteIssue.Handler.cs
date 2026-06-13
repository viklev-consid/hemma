using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.DeleteIssue;

public sealed class DeleteIssueHandler(IssuesOperations operations)
{
    public Task<ErrorOr<Deleted>> Handle(DeleteIssueCommand message, CancellationToken ct) => operations.DeleteIssueAsync(message, ct);
}
