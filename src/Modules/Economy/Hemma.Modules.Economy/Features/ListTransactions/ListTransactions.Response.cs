using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.ListTransactions;

public sealed record ListTransactionsResponse(
    IReadOnlyCollection<TransactionResponse> Transactions,
    int Page,
    int PageSize,
    int TotalCount);
