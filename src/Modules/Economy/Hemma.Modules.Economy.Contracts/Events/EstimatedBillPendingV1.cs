namespace Hemma.Modules.Economy.Contracts.Events;

public sealed record EstimatedBillPendingV1(
    Guid RecurringBillId,
    Guid OccurrenceId,
    Guid HouseholdId,
    Guid AccountId,
    Guid? CategoryId,
    decimal EstimatedAmount,
    string Currency,
    DateOnly DueOn,
    Guid EventId);
