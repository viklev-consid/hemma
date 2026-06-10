using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Gdpr;

public sealed class EconomyPersonalDataExporter(EconomyDbContext db) : IPersonalDataExporter
{
    public Task<PersonalDataExport> ExportAsync(UserRef user, CancellationToken ct) =>
        ExportAsync(user, householdId: null, ct);

    public async Task<PersonalDataExport> ExportAsync(UserRef user, Guid? householdId, CancellationToken ct)
    {
        var query = db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.PayerId == user.UserId);

        if (householdId.HasValue)
        {
            query = query.Where(transaction => transaction.HouseholdId == householdId.Value);
        }

        var transactions = await query
            .OrderBy(transaction => transaction.OccurredOn)
            .Select(transaction => new
            {
                transactionId = transaction.Id.Value,
                transaction.HouseholdId,
                accountId = transaction.AccountId.Value,
                categoryId = transaction.CategoryId == null ? (Guid?)null : transaction.CategoryId.Value,
                amount = transaction.Amount.Amount,
                currency = transaction.Amount.Currency,
                transaction.OccurredOn,
                transaction.Note,
                kind = transaction.Kind.Name,
                hasReceipt = transaction.HasReceipt,
                subscriptionId = transaction.SubscriptionId,
                transferId = transaction.TransferId == null ? (Guid?)null : transaction.TransferId.Value,
                transaction.IsTransferOutflow,
                transaction.IsPending,
            })
            .ToListAsync(ct);

        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["transactions"] = transactions,
        };

        return new PersonalDataExport(user.UserId, "Economy", data);
    }
}
