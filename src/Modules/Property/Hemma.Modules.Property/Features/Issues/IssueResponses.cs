using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Features.Projects;

namespace Hemma.Modules.Property.Features.Issues;

public sealed record IssueResponse(
    Guid IssueId,
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? AreaName,
    string Severity,
    string Status,
    DateTimeOffset ReportedAt,
    DateOnly? DueDate,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    Guid? LinkedProjectId,
    Guid? LinkedMaintenancePlanId,
    Guid? LinkedMaintenanceOccurrenceId,
    string? Notes)
{
    public static IssueResponse FromIssue(PropertyIssue issue) =>
        new(
            issue.Id.Value,
            issue.HouseholdId,
            issue.Title,
            issue.Description,
            issue.AreaId?.Value,
            null,
            issue.Severity.ToString(),
            issue.Status.ToString(),
            issue.ReportedAt,
            issue.DueDate,
            issue.ResolvedAt,
            issue.ClosedAt,
            issue.LinkedProjectId,
            issue.LinkedMaintenancePlanId,
            issue.LinkedMaintenanceOccurrenceId,
            issue.Notes);
}

public sealed record ListIssuesResponse(IReadOnlyList<IssueResponse> Issues);

public sealed record PromoteIssueToProjectResponse(IssueResponse Issue, ProjectResponse Project);
