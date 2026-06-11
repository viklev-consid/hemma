using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Analytics;

public sealed record MoneySeriesPointResponse(string Label, MoneyDto Value);

public sealed record MoneyCategorySeriesPointResponse(
    string Label,
    Guid CategoryId,
    string CategoryName,
    MoneyDto Value);

public sealed record CategoryTrendSeriesResponse(
    Guid CategoryId,
    string CategoryName,
    IReadOnlyCollection<MoneySeriesPointResponse> Points);

public sealed record GetCategoryTrendResponse(IReadOnlyCollection<CategoryTrendSeriesResponse> Series);

public sealed record SpendBreakdownSliceResponse(
    string Label,
    Guid CategoryId,
    string CategoryName,
    MoneyDto Value,
    decimal SharePercent);

public sealed record GetSpendBreakdownResponse(IReadOnlyCollection<SpendBreakdownSliceResponse> Slices);

public sealed record PeriodComparisonItemResponse(
    string Label,
    MoneyDto Current,
    MoneyDto Previous,
    MoneyDto Delta,
    decimal DeltaPercent);

public sealed record GetPeriodComparisonResponse(
    DateOnly CurrentPeriodStartsOn,
    DateOnly CurrentPeriodEndsOn,
    DateOnly PreviousPeriodStartsOn,
    DateOnly PreviousPeriodEndsOn,
    IReadOnlyCollection<PeriodComparisonItemResponse> Series);

public sealed record IncomeVsExpensePointResponse(
    string Label,
    MoneyDto Income,
    MoneyDto Expense,
    MoneyDto Net);

public sealed record GetIncomeVsExpenseResponse(IReadOnlyCollection<IncomeVsExpensePointResponse> Series);

public sealed record VarianceHistoryPointResponse(
    string Label,
    MoneyDto Planned,
    MoneyDto Actual,
    MoneyDto Variance);

public sealed record GetVarianceHistoryResponse(IReadOnlyCollection<VarianceHistoryPointResponse> Series);

public sealed record TopTransactionResponse(
    Guid TransactionId,
    DateOnly OccurredOn,
    Guid? CategoryId,
    string? CategoryName,
    MoneyDto Amount,
    string Kind,
    string? Note);

public sealed record GetTopTransactionsResponse(IReadOnlyCollection<TopTransactionResponse> Transactions);
