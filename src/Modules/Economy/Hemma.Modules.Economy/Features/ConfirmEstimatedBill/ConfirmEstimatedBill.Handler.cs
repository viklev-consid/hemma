using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.ConfirmEstimatedBill;

public sealed class ConfirmEstimatedBillHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<TransactionResponse>> Handle(ConfirmEstimatedBillCommand cmd, CancellationToken ct)
    {
        var billId = new RecurringBillId(cmd.RecurringBillId);
        var bill = await db.RecurringBills
            .Include(x => x.Occurrences)
            .SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == billId, ct);
        if (bill is null)
        {
            return EconomyErrors.RecurringBillNotFound;
        }

        var account = await db.Accounts.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == bill.AccountId, ct);
        if (account is null)
        {
            return EconomyErrors.AccountNotFound;
        }

        Category? category = null;
        if (bill.CategoryId is not null)
        {
            category = await db.Categories.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == bill.CategoryId, ct);
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

        var transaction = Transaction.Record(
            cmd.HouseholdId,
            account,
            category,
            amount.Value,
            cmd.OccurredOn,
            bill.Note ?? bill.Name,
            bill.Direction.ToTransactionKind(),
            payerId: null);
        if (transaction.IsError)
        {
            return transaction.Errors;
        }

        var billConfirmation = bill.ConfirmPending(cmd.OccurrenceId, transaction.Value);
        if (billConfirmation.IsError)
        {
            return billConfirmation.Errors;
        }

        db.Transactions.Add(transaction.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(transaction.Value.HouseholdId, "economy.recurring_bill.estimated_confirmed", "Transaction", transaction.Value.Id.Value, null, ct);
        return TransactionResponse.From(transaction.Value);
    }
}
