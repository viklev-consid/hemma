namespace Hemma.Modules.Economy.Contracts.Events;

public sealed record EstimatedBillPendingV1(
    Guid RecurringBillId,
    Guid? TransactionId,
    Guid HouseholdId,
    Guid AccountId,
    Guid? CategoryId,
    decimal EstimatedAmount,
    string Currency,
    DateOnly DueOn,
    Guid EventId);
