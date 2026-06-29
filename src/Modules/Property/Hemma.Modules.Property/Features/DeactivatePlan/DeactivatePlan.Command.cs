namespace Hemma.Modules.Property.Features.DeactivatePlan;

public sealed record DeactivatePlanCommand(Guid PlanId, Guid HouseholdId);
