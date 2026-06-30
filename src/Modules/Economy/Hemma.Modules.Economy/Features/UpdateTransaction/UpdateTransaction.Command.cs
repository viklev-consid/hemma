namespace Hemma.Modules.Economy.Features.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    Guid HouseholdId,
    Guid TransactionId,
    Guid AccountId,
    Guid? CategoryId,
    decimal Amount,
    string Currency,
    DateOnly OccurredOn,
    string? Note,
    string Kind,
    Guid? PayerId);
