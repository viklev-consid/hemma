using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    Guid HouseholdId,
    string Name,
    string? Description,
    Guid? AreaId,
    string? Priority,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    MoneyDto? BudgetEstimate,
    string? Notes);
