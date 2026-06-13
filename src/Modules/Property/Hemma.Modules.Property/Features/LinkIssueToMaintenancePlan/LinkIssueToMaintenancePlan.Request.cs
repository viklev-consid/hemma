namespace Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;

public sealed record LinkIssueRequest(Guid HouseholdId, Guid TargetId);
