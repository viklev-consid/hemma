using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Contracts;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
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

        var transactionId = new TransactionId(cmd.TransactionId);
        var transaction = await db.Transactions.SingleOrDefaultAsync(x =>
            x.HouseholdId == cmd.HouseholdId &&
            x.Id == transactionId, ct);
        if (transaction is null)
        {
            return EconomyErrors.TransactionNotFound;
        }

        var amount = Money.Create(cmd.Amount, cmd.Currency);
        if (amount.IsError)
        {
            return amount.Errors;
        }

        var confirmation = transaction.ConfirmPending(amount.Value, cmd.OccurredOn);
        if (confirmation.IsError)
        {
            return confirmation.Errors;
        }

        var billConfirmation = bill.ConfirmPending(transaction);
        if (billConfirmation.IsError)
        {
            return billConfirmation.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(transaction.HouseholdId, "economy.recurring_bill.estimated_confirmed", "Transaction", transaction.Id.Value, null, ct);
        return TransactionResponse.From(transaction);
    }
}
