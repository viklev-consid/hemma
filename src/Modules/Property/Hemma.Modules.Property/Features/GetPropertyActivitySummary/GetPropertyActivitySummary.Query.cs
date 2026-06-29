namespace Hemma.Modules.Property.Features.GetPropertyActivitySummary;

public sealed record GetPropertyActivitySummaryQuery(Guid HouseholdId, DateTimeOffset? Since);
