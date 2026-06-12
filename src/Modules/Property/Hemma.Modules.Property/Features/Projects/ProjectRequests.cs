using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Projects;

public sealed record ProjectRequest(
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

public sealed record ChangeProjectStatusRequest(Guid HouseholdId, string Status);

public sealed record ProjectTaskRequest(
    Guid HouseholdId,
    string Title,
    string Status,
    MoneyDto? Estimate,
    Guid? AssigneeId,
    DateOnly? DueDate);

public sealed record ReorderTasksRequest(Guid HouseholdId, IReadOnlyList<Guid> TaskIds);

public sealed record ProjectLinkRequest(Guid HouseholdId, string Label, string Url);
