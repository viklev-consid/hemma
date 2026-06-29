using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;

public sealed class LinkIssueToMaintenancePlanHandler(IssuesOperations operations)
{
    public Task<ErrorOr<IssueResponse>> Handle(LinkIssueToMaintenancePlanCommand message, CancellationToken ct) => operations.LinkIssueToMaintenancePlanAsync(message, ct);
}
