using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.UpdateIssue;

public sealed class UpdateIssueHandler(IssuesOperations operations)
{
    public Task<ErrorOr<IssueResponse>> Handle(UpdateIssueCommand message, CancellationToken ct) => operations.UpdateIssueAsync(message, ct);
}
