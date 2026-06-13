using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Issues;

public sealed record IssueRequest(
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? Severity,
    DateOnly? DueDate,
    string? Notes);

public sealed record ChangeIssueStatusRequest(Guid HouseholdId, string Status);

public sealed record LinkIssueRequest(Guid HouseholdId, Guid TargetId);

public sealed record PromoteIssueToProjectRequest(
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
