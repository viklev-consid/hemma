using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.Analytics;

public sealed class AnalyticsHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<GetCategoryTrendResponse>> Handle(GetCategoryTrendQuery query, CancellationToken ct)
    {
        var rows = await PostedTransactions(query.HouseholdId, query.From, query.To)
            .Where(transaction => transaction.Kind == TransactionKind.Expense && transaction.CategoryId != null)
            .GroupBy(transaction => new
            {
                transaction.OccurredOn.Year,
                transaction.OccurredOn.Month,
                transaction.CategoryId,
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                CategoryId = group.Key.CategoryId!.Value,
                Amount = group.Sum(transaction => transaction.Amount.Amount),
                Currency = group.Select(transaction => transaction.Amount.Currency).First(),
            })
            .ToListAsync(ct);

        var categories = await CategoryNamesAsync(query.HouseholdId, rows.Select(row => new CategoryId(row.CategoryId)).Distinct(), ct);
        var series = rows
            .GroupBy(row => row.CategoryId)
            .Select(group => new CategoryTrendSeriesResponse(
                group.Key,
                categories.GetValueOrDefault(new CategoryId(group.Key), "Uncategorized"),
                group
                    .OrderBy(row => row.Year)
                    .ThenBy(row => row.Month)
                    .Select(row => new MoneySeriesPointResponse(
                        PeriodLabel(row.Year, row.Month),
                        new MoneyDto(row.Amount, row.Currency)))
                    .ToArray()))
            .OrderBy(item => item.CategoryName, StringComparer.Ordinal)
            .ToArray();

        return new GetCategoryTrendResponse(series);
    }

    public async Task<ErrorOr<GetSpendBreakdownResponse>> Handle(GetSpendBreakdownQuery query, CancellationToken ct)
    {
        var transferModes = await db.Transfers
            .AsNoTracking()
            .Where(transfer => transfer.HouseholdId == query.HouseholdId)
            .ToDictionaryAsync(transfer => transfer.Id, transfer => transfer.Mode, ct);

        var transactions = await PostedTransactions(query.HouseholdId, query.From, query.To)
            .Where(transaction => transaction.CategoryId != null)
            .ToListAsync(ct);

        var included = transactions
            .Where(transaction => transaction.Kind == TransactionKind.Expense ||
                                  (transaction.Kind == TransactionKind.Transfer &&
                                   transaction.IsTransferOutflow &&
                                   transaction.TransferId is not null &&
                                   transferModes.TryGetValue(transaction.TransferId, out var mode) &&
                                   mode == TransferMode.Savings))
            .ToArray();

        var categories = await CategoryNamesAsync(query.HouseholdId, included.Select(row => row.CategoryId!).Distinct(), ct);
        var total = included.Sum(transaction => transaction.Amount.Amount);
        var slices = included
            .GroupBy(transaction => transaction.CategoryId!)
            .Select(group =>
            {
                var value = group.Sum(transaction => transaction.Amount.Amount);
                var name = categories.GetValueOrDefault(group.Key, "Uncategorized");
                return new SpendBreakdownSliceResponse(
                    name,
                    group.Key.Value,
                    name,
                    new MoneyDto(value, group.Select(transaction => transaction.Amount.Currency).First()),
                    total == 0 ? 0 : decimal.Round(value / total * 100, 2));
            })
            .OrderByDescending(slice => slice.Value.Amount)
            .ThenBy(slice => slice.CategoryName, StringComparer.Ordinal)
            .ToArray();

        return new GetSpendBreakdownResponse(slices.Length == 0 ? [] : slices);
    }

    public async Task<ErrorOr<GetPeriodComparisonResponse>> Handle(GetPeriodComparisonQuery query, CancellationToken ct)
    {
        var settings = await db.EconomySettings.AsNoTracking().SingleOrDefaultAsync(x => x.HouseholdId == query.HouseholdId, ct);
        if (settings is null)
        {
            return EconomyErrors.SettingsNotFound;
        }

        var current = BudgetPeriod.Containing(query.AnchorDate, settings.CycleStartDay);
        var previous = current.Previous();
        var currentTotal = await ExpenseTotalAsync(query.HouseholdId, current.StartsOn, current.EndsOn, ct);
        var previousTotal = await ExpenseTotalAsync(query.HouseholdId, previous.StartsOn, previous.EndsOn, ct);
        var currency = await CurrencyAsync(query.HouseholdId, ct);

        return new GetPeriodComparisonResponse(
            current.StartsOn,
            current.EndsOn,
            previous.StartsOn,
            previous.EndsOn,
            [
                new PeriodComparisonItemResponse(
                    "spend",
                    new MoneyDto(currentTotal, currency),
                    new MoneyDto(previousTotal, currency),
                    new MoneyDto(currentTotal - previousTotal, currency),
                    previousTotal == 0 ? 0 : decimal.Round((currentTotal - previousTotal) / previousTotal * 100, 2)),
            ]);
    }

    public async Task<ErrorOr<GetIncomeVsExpenseResponse>> Handle(GetIncomeVsExpenseQuery query, CancellationToken ct)
    {
        var rows = await PostedTransactions(query.HouseholdId, query.From, query.To)
            .Where(transaction => transaction.Kind == TransactionKind.Income || transaction.Kind == TransactionKind.Expense)
            .GroupBy(transaction => new
            {
                transaction.OccurredOn.Year,
                transaction.OccurredOn.Month,
                transaction.Kind,
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Kind = group.Key.Kind.Name,
                Amount = group.Sum(transaction => transaction.Amount.Amount),
                Currency = group.Select(transaction => transaction.Amount.Currency).First(),
            })
            .ToListAsync(ct);

        var currency = rows.FirstOrDefault()?.Currency ?? await CurrencyAsync(query.HouseholdId, ct);
        var series = rows
            .GroupBy(row => new { row.Year, row.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group =>
            {
                var income = group.Where(row => string.Equals(row.Kind, TransactionKind.Income.Name, StringComparison.Ordinal)).Sum(row => row.Amount);
                var expense = group.Where(row => string.Equals(row.Kind, TransactionKind.Expense.Name, StringComparison.Ordinal)).Sum(row => row.Amount);
                return new IncomeVsExpensePointResponse(
                    PeriodLabel(group.Key.Year, group.Key.Month),
                    new MoneyDto(income, currency),
                    new MoneyDto(expense, currency),
                    new MoneyDto(income - expense, currency));
            })
            .ToArray();

        return new GetIncomeVsExpenseResponse(series);
    }

    public async Task<ErrorOr<GetVarianceHistoryResponse>> Handle(GetVarianceHistoryQuery query, CancellationToken ct)
    {
        var transferModes = await db.Transfers
            .AsNoTracking()
            .Where(transfer => transfer.HouseholdId == query.HouseholdId)
            .ToDictionaryAsync(transfer => transfer.Id, transfer => transfer.Mode, ct);

        var budgets = await db.Budgets
            .AsNoTracking()
            .Include(budget => budget.Lines)
            .Where(budget => budget.HouseholdId == query.HouseholdId &&
                             budget.PeriodStartsOn <= query.To &&
                             budget.PeriodEndsOn >= query.From)
            .OrderBy(budget => budget.PeriodStartsOn)
            .ToListAsync(ct);

        var currency = budgets.SelectMany(budget => budget.Lines).Select(line => line.Amount.Currency).FirstOrDefault()
            ?? await CurrencyAsync(query.HouseholdId, ct);
        var series = new List<VarianceHistoryPointResponse>();
        foreach (var budget in budgets)
        {
            var lines = query.CategoryId is null
                ? budget.Lines.ToArray()
                : budget.Lines.Where(line => line.CategoryId == query.CategoryId).ToArray();
            if (lines.Length == 0)
            {
                continue;
            }

            var categoryIds = lines.Select(line => line.CategoryId).ToHashSet();
            var transactions = await PostedTransactions(query.HouseholdId, budget.PeriodStartsOn, budget.PeriodEndsOn)
                .Where(transaction => transaction.CategoryId != null)
                .ToListAsync(ct);

            var actual = transactions
                .Where(transaction => categoryIds.Contains(transaction.CategoryId!))
                .Where(transaction => transaction.Kind == TransactionKind.Expense ||
                                      (transaction.Kind == TransactionKind.Transfer &&
                                       transaction.IsTransferOutflow &&
                                       transaction.TransferId is not null &&
                                       transferModes.TryGetValue(transaction.TransferId, out var mode) &&
                                       mode == TransferMode.Savings))
                .Sum(transaction => transaction.Amount.Amount);
            var planned = lines.Sum(line => line.Amount.Amount);

            series.Add(new VarianceHistoryPointResponse(
                PeriodLabel(budget.PeriodStartsOn),
                new MoneyDto(planned, currency),
                new MoneyDto(actual, currency),
                new MoneyDto(planned - actual, currency)));
        }

        return new GetVarianceHistoryResponse(series);
    }

    public async Task<ErrorOr<GetTopTransactionsResponse>> Handle(GetTopTransactionsQuery query, CancellationToken ct)
    {
        var transactions = await PostedTransactions(query.HouseholdId, query.From, query.To)
            .Where(transaction => transaction.Kind != TransactionKind.Transfer)
            .Where(transaction => query.CategoryId == null || transaction.CategoryId == query.CategoryId)
            .OrderByDescending(transaction => transaction.Amount.Amount)
            .ThenByDescending(transaction => transaction.OccurredOn)
            .Take(Math.Clamp(query.Limit, 1, 100))
            .ToListAsync(ct);

        var categories = await CategoryNamesAsync(query.HouseholdId, transactions.Where(transaction => transaction.CategoryId != null).Select(transaction => transaction.CategoryId!), ct);
        return new GetTopTransactionsResponse(transactions.Select(transaction => new TopTransactionResponse(
            transaction.Id.Value,
            transaction.OccurredOn,
            transaction.CategoryId?.Value,
            transaction.CategoryId is null ? null : categories.GetValueOrDefault(transaction.CategoryId, "Uncategorized"),
            MoneyContract.From(transaction.Amount),
            transaction.Kind.Name,
            transaction.Note)).ToArray());
    }

    private IQueryable<Transaction> PostedTransactions(Guid householdId, DateOnly from, DateOnly to) =>
        db.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.HouseholdId == householdId &&
                                  transaction.OccurredOn >= from &&
                                  transaction.OccurredOn <= to &&
                                  !transaction.IsPending);

    private async Task<Dictionary<CategoryId, string>> CategoryNamesAsync(Guid householdId, IEnumerable<CategoryId> categoryIds, CancellationToken ct)
    {
        var ids = categoryIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await db.Categories
            .AsNoTracking()
            .Where(category => category.HouseholdId == householdId && ids.Contains(category.Id))
            .ToDictionaryAsync(category => category.Id, category => category.Name, ct);
    }

    private async Task<string> CurrencyAsync(Guid householdId, CancellationToken ct) =>
        await db.EconomySettings
            .AsNoTracking()
            .Where(settings => settings.HouseholdId == householdId)
            .Select(settings => settings.DefaultCurrency)
            .SingleOrDefaultAsync(ct) ?? "SEK";

    private async Task<decimal> ExpenseTotalAsync(Guid householdId, DateOnly from, DateOnly to, CancellationToken ct) =>
        await PostedTransactions(householdId, from, to)
            .Where(transaction => transaction.Kind == TransactionKind.Expense)
            .SumAsync(transaction => transaction.Amount.Amount, ct);

    private static string PeriodLabel(DateOnly startsOn) => PeriodLabel(startsOn.Year, startsOn.Month);

    private static string PeriodLabel(int year, int month) => $"{year:D4}-{month:D2}";
}
