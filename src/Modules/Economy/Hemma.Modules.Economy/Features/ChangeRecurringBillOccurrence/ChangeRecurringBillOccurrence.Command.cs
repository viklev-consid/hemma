namespace Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;

public sealed record ChangeRecurringBillOccurrenceCommand(
    Guid HouseholdId,
    Guid RecurringBillId,
    DateOnly DueOn,
    RecurringBillOccurrenceAction Action);
