using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed class SubscriptionHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<SubscriptionResponse>> Handle(CreateSubscriptionCommand cmd, CancellationToken ct)
    {
        Account? account = null;
        if (cmd.AccountId is not null)
        {
            var accountId = new AccountId(cmd.AccountId.Value);
            account = await db.Accounts.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == accountId, ct);
            if (account is null)
            {
                return EconomyErrors.AccountNotFound;
            }
        }

        var amount = Money.Create(cmd.ExpectedAmount, cmd.ExpectedCurrency);
        if (amount.IsError)
        {
            return amount.Errors;
        }

        var cadence = SubscriptionCadence.Create(cmd.CadenceFrequency, cmd.CadenceInterval, cmd.ChargeDay);
        if (cadence.IsError)
        {
            return cadence.Errors;
        }

        var state = SubscriptionLifecycleState.Create(cmd.LifecycleState);
        if (state.IsError)
        {
            return state.Errors;
        }

        var subscription = Subscription.Create(
            cmd.HouseholdId,
            cmd.Name,
            cadence.Value,
            amount.Value,
            state.Value,
            cmd.TrialEndsOn,
            account,
            cmd.StartsOn);
        if (subscription.IsError)
        {
            return subscription.Errors;
        }

        db.Subscriptions.Add(subscription.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(subscription.Value.HouseholdId, "economy.subscription.created", "Subscription", subscription.Value.Id.Value, null, ct);

        return SubscriptionResponse.From(subscription.Value);
    }

    public async Task<ErrorOr<SubscriptionResponse>> Handle(ChangeLifecycleStateCommand cmd, CancellationToken ct)
    {
        var subscriptionId = new SubscriptionId(cmd.SubscriptionId);
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == subscriptionId, ct);
        if (subscription is null)
        {
            return EconomyErrors.SubscriptionNotFound;
        }

        var state = SubscriptionLifecycleState.Create(cmd.LifecycleState);
        if (state.IsError)
        {
            return state.Errors;
        }

        var changed = subscription.ChangeLifecycleState(state.Value, cmd.TrialEndsOn);
        if (changed.IsError)
        {
            return changed.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(subscription.HouseholdId, "economy.subscription.lifecycle_changed", "Subscription", subscription.Id.Value, null, ct);
        return SubscriptionResponse.From(subscription);
    }

    public async Task<ErrorOr<TransactionResponse>> Handle(LinkTransactionCommand cmd, CancellationToken ct)
    {
        var subscriptionId = new SubscriptionId(cmd.SubscriptionId);
        if (!await db.Subscriptions.AnyAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == subscriptionId, ct))
        {
            return EconomyErrors.SubscriptionNotFound;
        }

        var transaction = await db.Transactions.SingleOrDefaultAsync(
            x => x.HouseholdId == cmd.HouseholdId && x.Id == new TransactionId(cmd.TransactionId),
            ct);
        if (transaction is null)
        {
            return EconomyErrors.TransactionNotFound;
        }

        var linked = transaction.LinkToSubscription(cmd.SubscriptionId);
        if (linked.IsError)
        {
            return linked.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(transaction.HouseholdId, "economy.subscription.transaction_linked", "Transaction", transaction.Id.Value, null, ct);
        return TransactionResponse.From(transaction);
    }

    public async Task<ErrorOr<TransactionResponse>> Handle(UnlinkTransactionCommand cmd, CancellationToken ct)
    {
        var subscriptionId = new SubscriptionId(cmd.SubscriptionId);
        if (!await db.Subscriptions.AnyAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == subscriptionId, ct))
        {
            return EconomyErrors.SubscriptionNotFound;
        }

        var transaction = await db.Transactions.SingleOrDefaultAsync(
            x => x.HouseholdId == cmd.HouseholdId && x.Id == new TransactionId(cmd.TransactionId),
            ct);
        if (transaction is null)
        {
            return EconomyErrors.TransactionNotFound;
        }

        var unlinked = transaction.UnlinkSubscription(cmd.SubscriptionId);
        if (unlinked.IsError)
        {
            return unlinked.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(transaction.HouseholdId, "economy.subscription.transaction_unlinked", "Transaction", transaction.Id.Value, null, ct);
        return TransactionResponse.From(transaction);
    }

    public async Task<ErrorOr<ChargeHistoryResponse>> Handle(GetChargeHistoryQuery query, CancellationToken ct)
    {
        var subscriptionId = new SubscriptionId(query.SubscriptionId);
        if (!await db.Subscriptions.AnyAsync(x => x.HouseholdId == query.HouseholdId && x.Id == subscriptionId, ct))
        {
            return EconomyErrors.SubscriptionNotFound;
        }

        var charges = await db.Transactions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId && x.SubscriptionId == query.SubscriptionId)
            .OrderBy(x => x.OccurredOn)
            .ThenBy(x => x.Id)
            .Select(x => new ChargeHistoryItemResponse(
                x.Id.Value,
                x.OccurredOn,
                new MoneyResponse(x.Amount.Amount, x.Amount.Currency),
                x.Note,
                "actual"))
            .ToListAsync(ct);

        var priceChanges = charges
            .Zip(charges.Skip(1))
            .Where(pair => pair.First.Amount.Amount != pair.Second.Amount.Amount ||
                           !string.Equals(pair.First.Amount.Currency, pair.Second.Amount.Currency, StringComparison.OrdinalIgnoreCase))
            .Select(pair => new PriceChangeResponse(pair.Second.OccurredOn, pair.First.Amount, pair.Second.Amount))
            .ToList();

        return new ChargeHistoryResponse(query.SubscriptionId, charges, priceChanges);
    }

    public async Task<ErrorOr<PaymentScheduleResponse>> Handle(GetPaymentScheduleQuery query, CancellationToken ct)
    {
        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId && x.LifecycleState != SubscriptionLifecycleState.Cancelled)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        var rows = subscriptions
            .Select(subscription => new SubscriptionPaymentScheduleResponse(
                subscription.Id.Value,
                subscription.Name,
                Enumerable.Range(1, 12)
                    .Where(month => subscription.Cadence.ChargesInMonth(subscription.StartsOn, query.Year, month))
                    .ToList()))
            .ToList();

        return new PaymentScheduleResponse(query.Year, rows);
    }

    public async Task<ErrorOr<MonthChargeCalendarResponse>> Handle(GetMonthChargeCalendarQuery query, CancellationToken ct)
    {
        var month = new DateOnly(query.Month.Year, query.Month.Month, 1);
        var lastDay = month.AddMonths(1).AddDays(-1);

        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId && x.LifecycleState != SubscriptionLifecycleState.Cancelled)
            .ToListAsync(ct);
        subscriptions = subscriptions
            .Where(x => x.Cadence.ChargesInMonth(x.StartsOn, month.Year, month.Month))
            .ToList();

        var actuals = await db.Transactions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId &&
                        x.SubscriptionId != null &&
                        x.OccurredOn >= month &&
                        x.OccurredOn <= lastDay)
            .Select(x => new
            {
                x.SubscriptionId,
                x.Id,
                x.OccurredOn,
                Amount = x.Amount.Amount,
                Currency = x.Amount.Currency
            })
            .ToListAsync(ct);

        var days = Enumerable.Range(1, lastDay.Day)
            .Select(day =>
            {
                var date = new DateOnly(month.Year, month.Month, day);
                var charges = subscriptions
                    .Where(subscription => subscription.Cadence.ChargeDay == day)
                    .Select(subscription =>
                    {
                        var actual = actuals.FirstOrDefault(x => x.SubscriptionId == subscription.Id.Value);
                        return actual is null
                            ? new MonthChargeResponse(subscription.Id.Value, subscription.Name, MoneyResponse.From(subscription.ExpectedAmount), "predicted", null)
                            : new MonthChargeResponse(subscription.Id.Value, subscription.Name, new MoneyResponse(actual.Amount, actual.Currency), "actual", actual.Id.Value);
                    })
                    .Where(x => string.Equals(x.MatchState, "actual", StringComparison.Ordinal) || x.Amount.Amount > 0)
                    .ToList();

                return new MonthChargeDayResponse(date, charges);
            })
            .Where(day => day.Charges.Count > 0)
            .ToList();

        return new MonthChargeCalendarResponse(month, days);
    }
}
