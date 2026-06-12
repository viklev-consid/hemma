using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.Maintenance;

public sealed record CreateMaintenancePlanCommand(
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string RecurrenceUnit,
    int RecurrenceInterval,
    DateOnly AnchorDate,
    int LeadTimeDays);

public sealed record UpdateMaintenancePlanCommand(
    Guid PlanId,
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string RecurrenceUnit,
    int RecurrenceInterval,
    DateOnly AnchorDate,
    int LeadTimeDays);

public sealed record DeactivatePlanCommand(Guid PlanId, Guid HouseholdId);

public sealed record DeletePlanCommand(Guid PlanId, Guid HouseholdId);

public sealed record CompleteOccurrenceCommand(Guid OccurrenceId, Guid HouseholdId, string? Notes);

public sealed record SkipOccurrenceCommand(Guid OccurrenceId, Guid HouseholdId, string? Notes);

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

public sealed record GetMaintenancePlanQuery(Guid PlanId, Guid HouseholdId);

public sealed record ListMaintenancePlansQuery(Guid HouseholdId, bool? ActiveOnly, Guid? AreaId, IReadOnlyList<Guid>? TagIds);

public sealed record ListUpcomingOccurrencesQuery(Guid HouseholdId, int HorizonDays);
