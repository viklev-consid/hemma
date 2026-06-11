using ErrorOr;
using Hemma.Shared.Contracts;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.SearchTransactionNote;

public sealed class SearchTransactionNoteHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<SearchTransactionNoteResponse>> Handle(SearchTransactionNoteQuery query, CancellationToken ct)
    {
        var search = EscapeLikePattern(query.Search.Trim());
        var transactions = db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.HouseholdId == query.HouseholdId &&
                                  transaction.Note != null &&
                                  EF.Functions.ILike(transaction.Note, $"%{search}%", "\\"));

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var total = await transactions.CountAsync(ct);
        var items = await transactions
            .OrderByDescending(transaction => transaction.OccurredOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new SearchTransactionNoteResponse(items.Select(TransactionResponse.From).ToArray(), page, pageSize, total);
    }

    private static string EscapeLikePattern(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
