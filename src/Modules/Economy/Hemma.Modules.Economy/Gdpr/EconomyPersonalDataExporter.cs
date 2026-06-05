using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Economy.Gdpr;

public sealed class EconomyPersonalDataExporter(EconomyDbContext db) : IPersonalDataExporter
{
    public async Task<PersonalDataExport> ExportAsync(UserRef user, CancellationToken ct)
    {
        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.PayerId == user.UserId)
            .OrderBy(transaction => transaction.OccurredOn)
            .ThenBy(transaction => transaction.Id.Value)
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

        var householdIds = transactions
            .Select(transaction => transaction.HouseholdId)
            .Distinct()
            .ToArray();

        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["transactions"] = transactions,
            ["households"] = await ExportHouseholdsAsync(householdIds, ct),
        };

        return new PersonalDataExport(user.UserId, "Economy", data);
    }

    public async Task<IReadOnlyDictionary<string, object?>> ExportHouseholdAsync(Guid householdId, CancellationToken ct)
    {
        var householdIds = new[] { householdId };
        return await ExportHouseholdsAsync(householdIds, ct);
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExportHouseholdsAsync(
        IReadOnlyCollection<Guid> householdIds,
        CancellationToken ct)
    {
        var settings = await db.EconomySettings
            .AsNoTracking()
            .Where(settings => householdIds.Contains(settings.HouseholdId))
            .OrderBy(settings => settings.HouseholdId)
            .Select(settings => new
            {
                settingsId = settings.Id.Value,
                settings.HouseholdId,
                settings.CycleStartDay,
                settings.DefaultCurrency,
            })
            .ToListAsync(ct);

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(account => householdIds.Contains(account.HouseholdId))
            .OrderBy(account => account.Name)
            .Select(account => new
            {
                accountId = account.Id.Value,
                account.HouseholdId,
                account.Name,
                type = account.Type.Name,
                openingBalance = account.OpeningBalance.Amount,
                currency = account.OpeningBalance.Currency,
            })
            .ToListAsync(ct);

        var categories = await db.Categories
            .AsNoTracking()
            .Where(category => householdIds.Contains(category.HouseholdId))
            .OrderBy(category => category.Name)
            .Select(category => new
            {
                categoryId = category.Id.Value,
                category.HouseholdId,
                category.Name,
                parentCategoryId = category.ParentCategoryId == null ? (Guid?)null : category.ParentCategoryId.Value,
                category.Budgetable,
            })
            .ToListAsync(ct);

        var budgetEntities = await db.Budgets
            .AsNoTracking()
            .Include(budget => budget.Lines)
            .Where(budget => householdIds.Contains(budget.HouseholdId))
            .OrderBy(budget => budget.PeriodStartsOn)
            .ToListAsync(ct);
        var budgets = budgetEntities
            .Select(budget => new
            {
                budgetId = budget.Id.Value,
                budget.HouseholdId,
                budget.PeriodStartsOn,
                budget.PeriodEndsOn,
                lines = budget.Lines
                    .OrderBy(line => line.CategoryId.Value)
                    .Select(line => new
                    {
                        budgetLineId = line.Id.Value,
                        categoryId = line.CategoryId.Value,
                        amount = line.Amount.Amount,
                        currency = line.Amount.Currency,
                    }),
            })
            .ToList();

        var recurringBillEntities = await db.RecurringBills
            .AsNoTracking()
            .Include(bill => bill.Occurrences)
            .Where(bill => householdIds.Contains(bill.HouseholdId))
            .OrderBy(bill => bill.Name)
            .ToListAsync(ct);
        var recurringBills = recurringBillEntities
            .Select(bill => new
            {
                recurringBillId = bill.Id.Value,
                bill.HouseholdId,
                bill.Name,
                accountId = bill.AccountId.Value,
                categoryId = bill.CategoryId == null ? (Guid?)null : bill.CategoryId.Value,
                amount = bill.Amount.Amount,
                currency = bill.Amount.Currency,
                cadenceFrequency = bill.Cadence.Frequency,
                cadenceInterval = bill.Cadence.Interval,
                cadenceDayOfMonth = bill.Cadence.DayOfMonth,
                type = bill.Type.Name,
                direction = bill.Direction.Name,
                bill.StartsOn,
                bill.NextDueOn,
                bill.Note,
                occurrences = bill.Occurrences
                    .OrderBy(occurrence => occurrence.DueOn)
                    .Select(occurrence => new
                    {
                        occurrenceId = occurrence.Id,
                        occurrence.DueOn,
                        state = occurrence.State.Name,
                        transactionId = occurrence.TransactionId == null ? (Guid?)null : occurrence.TransactionId.Value,
                    }),
            })
            .ToList();

        var rules = await db.CategorizationRules
            .AsNoTracking()
            .Where(rule => householdIds.Contains(rule.HouseholdId))
            .OrderBy(rule => rule.Pattern)
            .Select(rule => new
            {
                ruleId = rule.Id.Value,
                rule.HouseholdId,
                match = rule.Match.Name,
                rule.Pattern,
                targetCategoryId = rule.TargetCategoryId.Value,
                rule.Enabled,
            })
            .ToListAsync(ct);

        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(subscription => householdIds.Contains(subscription.HouseholdId))
            .OrderBy(subscription => subscription.Name)
            .Select(subscription => new
            {
                subscriptionId = subscription.Id.Value,
                subscription.HouseholdId,
                subscription.Name,
                cadenceFrequency = subscription.Cadence.Frequency,
                cadenceInterval = subscription.Cadence.Interval,
                cadenceChargeDay = subscription.Cadence.ChargeDay,
                expectedAmount = subscription.ExpectedAmount.Amount,
                expectedCurrency = subscription.ExpectedAmount.Currency,
                lifecycleState = subscription.LifecycleState.Name,
                subscription.TrialEndsOn,
                accountId = subscription.AccountId == null ? (Guid?)null : subscription.AccountId.Value,
                subscription.StartsOn,
            })
            .ToListAsync(ct);

        var transfers = await db.Transfers
            .AsNoTracking()
            .Where(transfer => householdIds.Contains(transfer.HouseholdId))
            .OrderBy(transfer => transfer.HouseholdId)
            .Select(transfer => new
            {
                transferId = transfer.Id.Value,
                transfer.HouseholdId,
                outflowTransactionId = transfer.OutflowTransactionId.Value,
                inflowTransactionId = transfer.InflowTransactionId.Value,
                mode = transfer.Mode.Name,
            })
            .ToListAsync(ct);

        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(transaction => householdIds.Contains(transaction.HouseholdId))
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
                payerId = transaction.PayerId,
                transferId = transaction.TransferId == null ? (Guid?)null : transaction.TransferId.Value,
                transaction.IsTransferOutflow,
                transaction.IsPending,
            })
            .ToListAsync(ct);

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["settings"] = settings,
            ["accounts"] = accounts,
            ["categories"] = categories,
            ["budgets"] = budgets,
            ["transactions"] = transactions,
            ["recurringBills"] = recurringBills,
            ["categorizationRules"] = rules,
            ["subscriptions"] = subscriptions,
            ["transfers"] = transfers,
        };
    }
}
