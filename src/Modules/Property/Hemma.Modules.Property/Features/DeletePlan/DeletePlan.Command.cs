namespace Hemma.Modules.Property.Features.DeletePlan;

public sealed record DeletePlanCommand(Guid PlanId, Guid HouseholdId);
