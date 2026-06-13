namespace Hemma.Modules.Property.Features.SkipOccurrence;

public sealed record SkipOccurrenceCommand(Guid OccurrenceId, Guid HouseholdId, string? Notes);
