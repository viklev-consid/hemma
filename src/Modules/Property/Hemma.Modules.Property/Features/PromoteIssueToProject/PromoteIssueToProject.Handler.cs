using ErrorOr;
using Hemma.Modules.Property.Features.Shared;

namespace Hemma.Modules.Property.Features.PromoteIssueToProject;

public sealed class PromoteIssueToProjectHandler(IssuesOperations operations)
{
    public Task<ErrorOr<PromoteIssueToProjectResponse>> Handle(PromoteIssueToProjectCommand message, CancellationToken ct) => operations.PromoteIssueToProjectAsync(message, ct);
}
