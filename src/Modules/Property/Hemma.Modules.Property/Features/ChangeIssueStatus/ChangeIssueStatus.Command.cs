namespace Hemma.Modules.Property.Features.ChangeIssueStatus;

public sealed record ChangeIssueStatusCommand(Guid IssueId, Guid HouseholdId, string Status);
