namespace Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;

public sealed record LinkIssueToMaintenanceOccurrenceCommand(Guid IssueId, Guid HouseholdId, Guid MaintenanceOccurrenceId);
