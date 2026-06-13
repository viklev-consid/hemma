namespace Hemma.Modules.Property.Features.DeleteIssue;

public sealed record DeleteIssueCommand(Guid IssueId, Guid HouseholdId);
