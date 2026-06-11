using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Contracts;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.GetBudgetSummary;

public sealed class GetBudgetSummaryHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<GetBudgetSummaryResponse>> Handle(GetBudgetSummaryQuery query, CancellationToken ct)
    {
        var settings = await db.EconomySettings.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == query.HouseholdId, ct);
        if (settings is null)
        {
            return EconomyErrors.SettingsNotFound;
        }

        var period = BudgetPeriod.Containing(query.AnchorDate, settings.CycleStartDay);
        var budget = await db.Budgets
            .AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.HouseholdId == query.HouseholdId && x.PeriodStartsOn == period.StartsOn, ct);
        if (budget is null)
        {
            return EconomyErrors.BudgetNotFound;
        }

        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId &&
                        x.OccurredOn >= budget.PeriodStartsOn &&
                        x.OccurredOn <= budget.PeriodEndsOn &&
                        !x.IsPending &&
                        x.CategoryId != null)
            .ToListAsync(ct);

        var transfers = await db.Transfers
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId)
            .ToDictionaryAsync(x => x.Id, ct);

        var elapsedPercent = CalculateElapsedPercent(budget.PeriodStartsOn, budget.PeriodEndsOn, query.AnchorDate);
        var lines = budget.Lines
            .OrderBy(line => line.CategoryId.Value)
            .Select(line =>
            {
                var actual = transactions
                    .Where(transaction => transaction.CategoryId == line.CategoryId)
                    .Where(transaction => transaction.Kind == TransactionKind.Expense ||
                                          (transaction.Kind == TransactionKind.Transfer &&
                                           transaction.IsTransferOutflow &&
                                           transaction.TransferId is not null &&
                                           transfers.TryGetValue(transaction.TransferId, out var transfer) &&
                                           transfer.Mode == TransferMode.Savings))
                    .Sum(transaction => transaction.Amount.Amount);
                var pacePercent = line.Amount.Amount == 0 ? 0 : decimal.Round(actual / line.Amount.Amount * 100, 2);
                return new BudgetSummaryLineResponse(
                    line.CategoryId.Value,
                    MoneyContract.From(line.Amount),
                    new MoneyDto(actual, line.Amount.Currency),
                    pacePercent,
                    pacePercent > elapsedPercent);
            })
            .ToArray();

        return new GetBudgetSummaryResponse(
            budget.Id.Value,
            budget.HouseholdId,
            budget.PeriodStartsOn,
            budget.PeriodEndsOn,
            elapsedPercent,
            lines);
    }

    private static decimal CalculateElapsedPercent(DateOnly startsOn, DateOnly endsOn, DateOnly anchorDate)
    {
        var clamped = anchorDate;
        if (clamped < startsOn)
        {
            clamped = startsOn;
        }
        else if (clamped > endsOn)
        {
            clamped = endsOn;
        }

        var elapsedDays = clamped.DayNumber - startsOn.DayNumber + 1;
        var totalDays = endsOn.DayNumber - startsOn.DayNumber + 1;
        return decimal.Round((decimal)elapsedDays / totalDays * 100, 2);
    }
}
