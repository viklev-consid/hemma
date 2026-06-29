using ErrorOr;
using Hemma.Modules.Economy.Contracts.Events;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Economy.Features.RecordTransaction;

public sealed class RecordTransactionHandler(EconomyDbContext db, IMessageBus bus, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<TransactionResponse>> Handle(RecordTransactionCommand cmd, CancellationToken ct)
    {
        var accountId = new AccountId(cmd.AccountId);
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == accountId, ct);
        if (account is null)
        {
            return EconomyErrors.AccountNotFound;
        }

        Category? category = null;
        if (cmd.CategoryId is not null)
        {
            var categoryId = new CategoryId(cmd.CategoryId.Value);
            category = await db.Categories.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == categoryId, ct);
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

        var transaction = Transaction.Record(cmd.HouseholdId, account, category, amount.Value, cmd.OccurredOn, cmd.Note, kind.Value, cmd.PayerId);
        if (transaction.IsError)
        {
            return transaction.Errors;
        }

        db.Transactions.Add(transaction.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(transaction.Value.HouseholdId, "economy.transaction.recorded", "Transaction", transaction.Value.Id.Value, null, ct);

        if (transaction.Value.Kind == TransactionKind.Expense)
        {
            await bus.PublishAsync(new ExpenseRecordedV1(
                transaction.Value.Id.Value,
                transaction.Value.HouseholdId,
                transaction.Value.AccountId.Value,
                transaction.Value.CategoryId?.Value,
                transaction.Value.Amount.Amount,
                transaction.Value.Amount.Currency,
                transaction.Value.OccurredOn,
                Guid.NewGuid()));
            await PublishBudgetExceededIfNeededAsync(transaction.Value, ct);
        }

        return TransactionResponse.From(transaction.Value);
    }

    private async Task PublishBudgetExceededIfNeededAsync(Transaction transaction, CancellationToken ct)
    {
        if (transaction.CategoryId is null)
        {
            return;
        }

        var budget = await db.Budgets
            .Include(x => x.Lines)
            .Where(x => x.HouseholdId == transaction.HouseholdId &&
                        x.PeriodStartsOn <= transaction.OccurredOn &&
                        x.PeriodEndsOn >= transaction.OccurredOn)
            .SingleOrDefaultAsync(ct);
        var line = budget?.Lines.SingleOrDefault(x => x.CategoryId == transaction.CategoryId);
        if (budget is null || line is null)
        {
            return;
        }

        var actual = await db.Transactions
            .Where(x => x.HouseholdId == transaction.HouseholdId &&
                        x.CategoryId == transaction.CategoryId &&
                        x.OccurredOn >= budget.PeriodStartsOn &&
                        x.OccurredOn <= budget.PeriodEndsOn &&
                        !x.IsPending &&
                        x.Kind == TransactionKind.Expense)
            .SumAsync(x => x.Amount.Amount, ct);

        if (actual > line.Amount.Amount)
        {
            await bus.PublishAsync(new BudgetExceededV1(
                transaction.HouseholdId,
                budget.Id.Value,
                transaction.CategoryId.Value,
                line.Amount.Amount,
                actual,
                line.Amount.Currency,
                Guid.NewGuid()));
        }
    }
}
