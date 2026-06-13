using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;

public sealed class LinkIssueToMaintenanceOccurrenceHandler(IssuesOperations operations)
{
    public Task<ErrorOr<IssueResponse>> Handle(LinkIssueToMaintenanceOccurrenceCommand message, CancellationToken ct) => operations.LinkIssueToMaintenanceOccurrenceAsync(message, ct);
}
