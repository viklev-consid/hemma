namespace Hemma.Modules.Property.Features.ClearOccurrenceSnooze;

public sealed record ClearOccurrenceSnoozeCommand(Guid OccurrenceId, Guid HouseholdId);
