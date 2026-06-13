namespace Hemma.Modules.Property.Features.ChangeIssueStatus;

public sealed record ChangeIssueStatusRequest(Guid HouseholdId, string Status);
