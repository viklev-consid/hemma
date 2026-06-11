using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Contracts;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed class SubscriptionHandler(EconomyDbContext db, EconomyAuditPublisher audit, IClock clock)
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

        var changed = subscription.ChangeLifecycleState(state.Value, cmd.TrialEndsOn, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime));
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

    public async Task<ErrorOr<ListSubscriptionsResponse>> Handle(ListSubscriptionsQuery query, CancellationToken ct)
    {
        var subscriptions = await db.Subscriptions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return new ListSubscriptionsResponse(subscriptions.Select(SubscriptionResponse.From).ToList());
    }

    public async Task<ErrorOr<SubscriptionResponse>> Handle(GetSubscriptionQuery query, CancellationToken ct)
    {
        var subscriptionId = new SubscriptionId(query.SubscriptionId);
        var subscription = await db.Subscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.HouseholdId == query.HouseholdId && x.Id == subscriptionId, ct);

        return subscription is null
            ? EconomyErrors.SubscriptionNotFound
            : SubscriptionResponse.From(subscription);
    }

    public async Task<ErrorOr<LinkCandidatesResponse>> Handle(GetLinkCandidatesQuery query, CancellationToken ct)
    {
        var subscriptionId = new SubscriptionId(query.SubscriptionId);
        var subscription = await db.Subscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.HouseholdId == query.HouseholdId && x.Id == subscriptionId, ct);
        if (subscription is null)
        {
            return EconomyErrors.SubscriptionNotFound;
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var windowStart = today.AddMonths(-12);
        if (windowStart < subscription.StartsOn)
        {
            windowStart = subscription.StartsOn;
        }

        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId &&
                        x.SubscriptionId == null &&
                        x.OccurredOn >= windowStart)
            .Select(x => new
            {
                x.Id,
                x.OccurredOn,
                Amount = x.Amount.Amount,
                Currency = x.Amount.Currency,
                x.Note
            })
            .ToListAsync(ct);

        var candidates = transactions
            .Select(x => new
            {
                Transaction = x,
                Match = SubscriptionChargeMatcher.Match(subscription, x.OccurredOn, x.Amount, x.Note)
            })
            .Where(x => x.Match is not null)
            .OrderByDescending(x => x.Transaction.OccurredOn)
            .ThenBy(x => x.Match!.DayDelta)
            .ThenBy(x => x.Match!.AmountDelta)
            .Take(10)
            .Select(x => new LinkCandidateResponse(
                x.Transaction.Id.Value,
                x.Transaction.OccurredOn,
                new MoneyDto(x.Transaction.Amount, x.Transaction.Currency),
                x.Transaction.Note))
            .ToList();

        return new LinkCandidatesResponse(query.SubscriptionId, candidates);
    }

    public async Task<ErrorOr<ChargeHistoryResponse>> Handle(GetChargeHistoryQuery query, CancellationToken ct)
    {
        var subscriptionId = new SubscriptionId(query.SubscriptionId);
        if (!await db.Subscriptions.AnyAsync(x => x.HouseholdId == query.HouseholdId && x.Id == subscriptionId, ct))
        {
            return EconomyErrors.SubscriptionNotFound;
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var queryableCharges = db.Transactions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId && x.SubscriptionId == query.SubscriptionId);

        var total = await queryableCharges.CountAsync(ct);
        var includePrevious = skip > 0;
        var windowSkip = includePrevious ? skip - 1 : skip;
        var windowTake = pageSize + (includePrevious ? 1 : 0);
        var window = await queryableCharges
            .OrderBy(x => x.OccurredOn)
            .ThenBy(x => x.Id)
            .Skip(windowSkip)
            .Take(windowTake)
            .Select(x => new ChargeHistoryItemResponse(
                x.Id.Value,
                x.OccurredOn,
                new MoneyDto(x.Amount.Amount, x.Amount.Currency),
                x.Note,
                "actual"))
            .ToListAsync(ct);

        var charges = includePrevious ? window.Skip(1).ToList() : window;
        var comparisons = includePrevious
            ? window.Zip(window.Skip(1))
            : charges.Zip(charges.Skip(1));
        var priceChanges = comparisons
            .Where(pair => pair.First.Amount.Amount != pair.Second.Amount.Amount ||
                           !string.Equals(pair.First.Amount.Currency, pair.Second.Amount.Currency, StringComparison.OrdinalIgnoreCase))
            .Select(pair => new PriceChangeResponse(pair.Second.OccurredOn, pair.First.Amount, pair.Second.Amount))
            .ToList();

        return new ChargeHistoryResponse(query.SubscriptionId, charges, priceChanges, page, pageSize, total);
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
            .Where(x => x.HouseholdId == query.HouseholdId)
            .ToListAsync(ct);
        var subscriptionsById = subscriptions.ToDictionary(x => x.Id.Value);

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
        actuals = actuals.Where(x => subscriptionsById.ContainsKey(x.SubscriptionId!.Value)).ToList();

        var subscriptionIdsWithActuals = actuals.Select(x => x.SubscriptionId!.Value).ToHashSet();
        var predictedSubscriptions = subscriptions
            .Where(x => x.LifecycleState != SubscriptionLifecycleState.Cancelled &&
                        x.Cadence.ChargesInMonth(x.StartsOn, month.Year, month.Month) &&
                        x.ExpectedAmount.Amount > 0 &&
                        !subscriptionIdsWithActuals.Contains(x.Id.Value))
            .ToList();

        var days = Enumerable.Range(1, lastDay.Day)
            .Select(day =>
            {
                var date = new DateOnly(month.Year, month.Month, day);
                var actualCharges = actuals
                    .Where(x => x.OccurredOn == date)
                    .Select(x => new MonthChargeResponse(
                        x.SubscriptionId!.Value,
                        subscriptionsById[x.SubscriptionId!.Value].Name,
                        new MoneyDto(x.Amount, x.Currency),
                        "actual",
                        x.Id.Value));
                var predictedCharges = predictedSubscriptions
                    .Where(x => x.Cadence.ChargeDay == day)
                    .Select(x => new MonthChargeResponse(x.Id.Value, x.Name, MoneyContract.From(x.ExpectedAmount), "predicted", null));

                return new MonthChargeDayResponse(date, actualCharges.Concat(predictedCharges).ToList());
            })
            .Where(day => day.Charges.Count > 0)
            .ToList();

        var charges = days.SelectMany(day => day.Charges).ToList();
        var currency = charges.FirstOrDefault()?.Amount.Currency
            ?? await db.EconomySettings
                .AsNoTracking()
                .Where(x => x.HouseholdId == query.HouseholdId)
                .Select(x => x.DefaultCurrency)
                .SingleOrDefaultAsync(ct)
            ?? "SEK";
        var actualTotal = charges
            .Where(x => string.Equals(x.MatchState, "actual", StringComparison.Ordinal))
            .Sum(x => x.Amount.Amount);
        var predictedTotal = charges
            .Where(x => string.Equals(x.MatchState, "predicted", StringComparison.Ordinal))
            .Sum(x => x.Amount.Amount);

        return new MonthChargeCalendarResponse(
            month,
            days,
            new MoneyDto(actualTotal, currency),
            new MoneyDto(predictedTotal, currency));
    }
}
