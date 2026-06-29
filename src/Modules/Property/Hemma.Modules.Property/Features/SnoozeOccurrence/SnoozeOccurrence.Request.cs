namespace Hemma.Modules.Property.Features.SnoozeOccurrence;

public sealed record SnoozeOccurrenceRequest(
    Guid HouseholdId,
    DateOnly SnoozedUntil,
    string? Reason);
