namespace Hemma.Modules.Economy.Features.ListTransactionsForProject;

public sealed record ListTransactionsForProjectResponse(
    IReadOnlyCollection<TransactionResponse> Transactions,
    int Page,
    int PageSize,
    int TotalCount);
