using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.PromoteIssueToProject;

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
