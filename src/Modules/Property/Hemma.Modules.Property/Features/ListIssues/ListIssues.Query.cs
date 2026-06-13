namespace Hemma.Modules.Property.Features.ListIssues;

public sealed record ListIssuesQuery(
    Guid HouseholdId,
    string? Status,
    Guid? AreaId,
    string? Severity,
    IReadOnlyList<Guid>? TagIds,
    bool? IsOverdue,
    Guid? LinkedProjectId);
