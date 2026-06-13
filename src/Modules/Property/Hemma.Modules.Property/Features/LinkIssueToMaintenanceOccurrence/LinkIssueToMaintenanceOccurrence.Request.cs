namespace Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;

public sealed record LinkIssueRequest(Guid HouseholdId, Guid TargetId);
