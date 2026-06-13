namespace Hemma.Modules.Property.Features.ListPropertyActivity;

public sealed record ListPropertyActivityQuery(
    Guid HouseholdId,
    DateTimeOffset? Since,
    string? TargetType,
    Guid? TargetId,
    int? Limit);
