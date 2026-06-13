namespace Hemma.Modules.Property.Features.CreateMaintenancePlan;

public sealed record CreateMaintenancePlanCommand(
    Guid HouseholdId,
    string Title,
    string? Description,
    Guid? AreaId,
    string RecurrenceUnit,
    int RecurrenceInterval,
    DateOnly AnchorDate,
    int LeadTimeDays);
