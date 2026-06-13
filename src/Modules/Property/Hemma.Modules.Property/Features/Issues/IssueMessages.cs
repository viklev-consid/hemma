using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Issues;

public sealed record ReportIssueCommand(
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? Severity,
    DateOnly? DueDate,
    string? Notes);

public sealed record UpdateIssueCommand(
    Guid IssueId,
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? Severity,
    DateOnly? DueDate,
    string? Notes);

public sealed record ChangeIssueStatusCommand(Guid IssueId, Guid HouseholdId, string Status);

public sealed record DeleteIssueCommand(Guid IssueId, Guid HouseholdId);

public sealed record LinkIssueToMaintenancePlanCommand(Guid IssueId, Guid HouseholdId, Guid MaintenancePlanId);

public sealed record LinkIssueToMaintenanceOccurrenceCommand(Guid IssueId, Guid HouseholdId, Guid MaintenanceOccurrenceId);

public sealed record UnlinkIssueCommand(Guid IssueId, Guid HouseholdId);

public sealed record PromoteIssueToProjectCommand(
    Guid IssueId,
    Guid HouseholdId,
    string Name,
    string? Description,
    string Status,
    Guid? AreaId,
    string? Priority,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    MoneyDto? BudgetEstimate,
    string? Notes);

public sealed record GetIssueQuery(Guid IssueId, Guid HouseholdId);

public sealed record ListIssuesQuery(
    Guid HouseholdId,
    string? Status,
    Guid? AreaId,
    string? Severity,
    IReadOnlyList<Guid>? TagIds,
    bool? IsOverdue,
    Guid? LinkedProjectId);
