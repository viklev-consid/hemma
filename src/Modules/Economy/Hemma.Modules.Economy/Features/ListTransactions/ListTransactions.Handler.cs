using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.ListTransactions;

public sealed class ListTransactionsHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<ListTransactionsResponse>> Handle(ListTransactionsQuery query, CancellationToken ct)
    {
        var transactions = db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.HouseholdId == query.HouseholdId);

        if (query.CategoryId is not null)
        {
            var categoryId = new CategoryId(query.CategoryId.Value);
            transactions = transactions.Where(transaction => transaction.CategoryId == categoryId);
        }

        if (query.From is not null)
        {
            transactions = transactions.Where(transaction => transaction.OccurredOn >= query.From.Value);
        }

        if (query.To is not null)
        {
            transactions = transactions.Where(transaction => transaction.OccurredOn <= query.To.Value);
        }

        if (query.PayerId is not null)
        {
            transactions = transactions.Where(transaction => transaction.PayerId == query.PayerId);
        }

        if (query.HasReceipt is not null)
        {
            transactions = query.HasReceipt.Value
                ? transactions.Where(transaction => transaction.ReceiptBlobKey != null)
                : transactions.Where(transaction => transaction.ReceiptBlobKey == null);
        }

        if (query.MinAmount is not null)
        {
            transactions = transactions.Where(transaction => transaction.Amount.Amount >= query.MinAmount.Value);
        }

        if (query.MaxAmount is not null)
        {
            transactions = transactions.Where(transaction => transaction.Amount.Amount <= query.MaxAmount.Value);
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var total = await transactions.CountAsync(ct);
        var items = await transactions
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListTransactionsResponse(items.Select(TransactionResponse.From).ToArray(), page, pageSize, total);
    }
}
