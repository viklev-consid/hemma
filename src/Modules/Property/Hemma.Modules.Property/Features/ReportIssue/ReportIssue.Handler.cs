using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.ReportIssue;

public sealed class ReportIssueHandler(IssuesOperations operations)
{
    public Task<ErrorOr<IssueResponse>> Handle(ReportIssueCommand message, CancellationToken ct) => operations.ReportIssueAsync(message, ct);
}
