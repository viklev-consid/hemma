namespace Hemma.Modules.Property.Features.GetPropertyActivitySummary;

public sealed record PropertyActivityCountResponse(string Key, int Count);

public sealed record PropertyActivitySummaryResponse(
    Guid HouseholdId,
    DateTimeOffset? Since,
    IReadOnlyList<PropertyActivityCountResponse> ByVerb,
    IReadOnlyList<PropertyActivityCountResponse> ByTargetType);
