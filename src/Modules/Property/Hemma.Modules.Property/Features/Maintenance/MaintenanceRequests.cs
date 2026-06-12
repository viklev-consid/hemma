using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Maintenance;

public sealed record MaintenancePlanRequest(
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string RecurrenceUnit,
    int RecurrenceInterval,
    DateOnly AnchorDate,
    int LeadTimeDays);

public sealed record CompleteOccurrenceRequest(Guid HouseholdId, string? Notes);

public sealed record SkipOccurrenceRequest(Guid HouseholdId, string? Notes);

public sealed record PromoteOccurrenceRequest(
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
