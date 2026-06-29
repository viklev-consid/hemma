using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.SearchTransactionNote;

public sealed record SearchTransactionNoteResponse(IReadOnlyCollection<TransactionResponse> Transactions, int Page, int PageSize, int TotalCount);
