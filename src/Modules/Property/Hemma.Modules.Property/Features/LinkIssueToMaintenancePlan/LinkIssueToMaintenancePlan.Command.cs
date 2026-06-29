namespace Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;

public sealed record LinkIssueToMaintenancePlanCommand(Guid IssueId, Guid HouseholdId, Guid MaintenancePlanId);
