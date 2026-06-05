namespace Hemma.Modules.Economy.Features.RecordTransaction;

public sealed record RecordTransactionCommand(
    Guid HouseholdId,
    Guid AccountId,
    Guid? CategoryId,
    decimal Amount,
    string Currency,
    DateOnly OccurredOn,
    string? Note,
    string Kind,
    Guid? PayerId);
