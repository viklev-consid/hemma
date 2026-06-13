namespace Hemma.Modules.Property.Features.SnoozeOccurrence;

public sealed record SnoozeOccurrenceCommand(
    Guid OccurrenceId,
    Guid HouseholdId,
    DateOnly SnoozedUntil,
    string? Reason);
