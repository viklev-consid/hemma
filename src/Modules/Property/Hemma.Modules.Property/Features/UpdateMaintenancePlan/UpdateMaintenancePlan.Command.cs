namespace Hemma.Modules.Property.Features.UpdateMaintenancePlan;

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
