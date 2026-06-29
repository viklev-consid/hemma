using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.RecordTransaction;

public sealed record RecordTransactionRequest(
    Guid HouseholdId,
    Guid AccountId,
    Guid? CategoryId,
    MoneyDto Amount,
    DateOnly OccurredOn,
    string? Note,
    string Kind,
    Guid? PayerId);
