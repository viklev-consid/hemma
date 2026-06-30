using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.UpdateTransaction;

public sealed class UpdateTransactionHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<TransactionResponse>> Handle(UpdateTransactionCommand cmd, CancellationToken ct)
    {
        var transactionId = new TransactionId(cmd.TransactionId);
        var transaction = await db.Transactions
            .SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == transactionId, ct);
        if (transaction is null)
        {
            return EconomyErrors.TransactionNotFound;
        }

        var accountId = new AccountId(cmd.AccountId);
        var account = await db.Accounts
            .SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == accountId, ct);
        if (account is null)
        {
            return EconomyErrors.AccountNotFound;
        }

        Category? category = null;
        if (cmd.CategoryId is { } categoryValue)
        {
            var categoryId = new CategoryId(categoryValue);
            category = await db.Categories
                .SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == categoryId, ct);
            if (category is null)
            {
                return EconomyErrors.CategoryNotFound;
            }
        }

        var amount = Money.Create(cmd.Amount, cmd.Currency);
        if (amount.IsError)
        {
            return amount.Errors;
        }

        var kind = TransactionKind.Create(cmd.Kind);
        if (kind.IsError || kind.Value == TransactionKind.Transfer)
        {
            return EconomyErrors.TransactionKindInvalid;
        }

        var updated = transaction.UpdateDetails(
            account,
            category,
            amount.Value,
            cmd.OccurredOn,
            cmd.Note,
            kind.Value,
            cmd.PayerId);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(transaction.HouseholdId, "economy.transaction.updated", "Transaction", transaction.Id.Value, null, ct);

        return TransactionResponse.From(transaction);
    }
}
