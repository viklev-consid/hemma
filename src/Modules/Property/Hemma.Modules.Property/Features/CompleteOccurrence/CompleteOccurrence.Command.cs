namespace Hemma.Modules.Property.Features.CompleteOccurrence;

public sealed record CompleteOccurrenceCommand(Guid OccurrenceId, Guid HouseholdId, string? Notes);
