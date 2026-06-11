using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.RecordTransaction;

public sealed record RecordTransactionRequest(
    Guid HouseholdId,
    Guid AccountId,
    Guid? CategoryId,
    MoneyRequest Amount,
    DateOnly OccurredOn,
    string? Note,
    string Kind,
    Guid? PayerId);
