namespace Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;

public sealed record ChangeRecurringBillOccurrenceRequest(Guid HouseholdId, DateOnly DueOn);
