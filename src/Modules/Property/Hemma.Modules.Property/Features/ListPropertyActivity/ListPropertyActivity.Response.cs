namespace Hemma.Modules.Property.Features.ListPropertyActivity;

public sealed record PropertyActivityItemResponse(
    Guid ActivityId,
    Guid HouseholdId,
    DateTimeOffset OccurredAt,
    Guid? ActorId,
    string Verb,
    string TargetType,
    Guid TargetId,
    string Summary,
    IReadOnlyDictionary<string, string?> Metadata);

public sealed record ListPropertyActivityResponse(IReadOnlyList<PropertyActivityItemResponse> Items);
