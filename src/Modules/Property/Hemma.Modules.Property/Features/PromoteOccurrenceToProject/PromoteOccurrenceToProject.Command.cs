using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.PromoteOccurrenceToProject;

public sealed record PromoteOccurrenceToProjectCommand(
    Guid OccurrenceId,
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
