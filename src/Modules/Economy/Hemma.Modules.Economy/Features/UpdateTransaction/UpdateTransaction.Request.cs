using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.UpdateTransaction;

public sealed record UpdateTransactionRequest(
    Guid HouseholdId,
    Guid AccountId,
    Guid? CategoryId,
    MoneyDto Amount,
    DateOnly OccurredOn,
    string? Note,
    string Kind,
    Guid? PayerId);
