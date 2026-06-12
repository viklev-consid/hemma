using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Features.Projects;

namespace Hemma.Modules.Property.Features.Maintenance;

public sealed record MaintenancePlanResponse(
    Guid PlanId,
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string? AreaName,
    string RecurrenceUnit,
    int RecurrenceInterval,
    DateOnly AnchorDate,
    int LeadTimeDays,
    bool IsActive)
{
    public static MaintenancePlanResponse FromPlan(MaintenancePlan plan) =>
        new(
            plan.Id.Value,
            plan.HouseholdId,
            plan.Title,
            plan.Description,
            plan.AreaId?.Value,
            null,
            plan.RecurrenceUnit.ToString(),
            plan.RecurrenceInterval,
            plan.AnchorDate,
            plan.LeadTimeDays,
            plan.IsActive);
}

public sealed record ListMaintenancePlansResponse(IReadOnlyList<MaintenancePlanResponse> Plans);

public sealed record MaintenanceOccurrenceResponse(
    Guid OccurrenceId,
    Guid PlanId,
    Guid HouseholdId,
    DateOnly DueDate,
    string Status,
    DateTimeOffset? CompletedAt,
    string? Notes,
    Guid? SpawnedProjectId)
{
    public static MaintenanceOccurrenceResponse FromOccurrence(MaintenanceOccurrence occurrence) =>
        new(
            occurrence.Id.Value,
            occurrence.PlanId.Value,
            occurrence.HouseholdId,
            occurrence.DueDate,
            occurrence.Status.ToString(),
            occurrence.CompletedAt,
            occurrence.Notes,
            occurrence.SpawnedProjectId);
}

public sealed record GetMaintenancePlanResponse(
    MaintenancePlanResponse Plan,
    MaintenanceOccurrenceResponse? NextOccurrence);

public sealed record UpcomingOccurrenceItem(
    Guid OccurrenceId,
    Guid PlanId,
    Guid HouseholdId,
    string PlanTitle,
    Guid? AreaId,
    string? AreaName,
    DateOnly DueDate,
    string Status);

public sealed record ListUpcomingOccurrencesResponse(IReadOnlyList<UpcomingOccurrenceItem> Occurrences);

public sealed record CompleteOccurrenceResponse(
    MaintenanceOccurrenceResponse Occurrence,
    SuggestedHistoryEntryResponse? SuggestedHistoryEntry,
    MaintenanceOccurrenceResponse? NextOccurrence);

public sealed record SkipOccurrenceResponse(
    MaintenanceOccurrenceResponse Occurrence,
    MaintenanceOccurrenceResponse? NextOccurrence);

public sealed record PromoteOccurrenceResponse(
    MaintenanceOccurrenceResponse Occurrence,
    ProjectResponse Project,
    MaintenanceOccurrenceResponse? NextOccurrence);
