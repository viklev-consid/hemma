namespace Hemma.Modules.Economy.Features.ListTransactions;

public sealed record ListTransactionsQuery(
    Guid HouseholdId,
    Guid? CategoryId,
    DateOnly? From,
    DateOnly? To,
    Guid? PayerId,
    bool? HasReceipt,
    decimal? MinAmount,
    decimal? MaxAmount,
    int Page,
    int PageSize);
