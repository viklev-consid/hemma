namespace Hemma.Modules.Economy.Contracts.Events;

public sealed record ExpenseRecordedV1(
    Guid TransactionId,
    Guid HouseholdId,
    Guid AccountId,
    Guid? CategoryId,
    decimal Amount,
    string Currency,
    DateOnly OccurredOn,
    Guid EventId);
