using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.ListTransactionsForProject;

public sealed class ListTransactionsForProjectHandler(EconomyDbContext db)
{
    public async Task<ListTransactionsForProjectResponse> Handle(ListTransactionsForProjectQuery query, CancellationToken ct)
    {
        var transactions = db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.HouseholdId == query.HouseholdId && transaction.ProjectId == query.ProjectId);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var total = await transactions.CountAsync(ct);
        var items = await transactions
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListTransactionsForProjectResponse(items.Select(TransactionResponse.From).ToArray(), page, pageSize, total);
    }
}
