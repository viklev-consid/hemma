namespace Hemma.Modules.Property.Features.UnlinkIssue;

public sealed record UnlinkIssueCommand(Guid IssueId, Guid HouseholdId);
