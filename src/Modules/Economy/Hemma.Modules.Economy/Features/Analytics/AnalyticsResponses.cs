using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Analytics;

public sealed record MoneySeriesPointResponse(string Label, MoneyResponse Value);

public sealed record MoneyCategorySeriesPointResponse(
    string Label,
    Guid CategoryId,
    string CategoryName,
    MoneyResponse Value);

public sealed record CategoryTrendSeriesResponse(
    Guid CategoryId,
    string CategoryName,
    IReadOnlyCollection<MoneySeriesPointResponse> Points);

public sealed record GetCategoryTrendResponse(IReadOnlyCollection<CategoryTrendSeriesResponse> Series);

public sealed record SpendBreakdownSliceResponse(
    string Label,
    Guid CategoryId,
    string CategoryName,
    MoneyResponse Value,
    decimal SharePercent);

public sealed record GetSpendBreakdownResponse(IReadOnlyCollection<SpendBreakdownSliceResponse> Slices);

public sealed record PeriodComparisonItemResponse(
    string Label,
    MoneyResponse Current,
    MoneyResponse Previous,
    MoneyResponse Delta,
    decimal DeltaPercent);

public sealed record GetPeriodComparisonResponse(
    DateOnly CurrentPeriodStartsOn,
    DateOnly CurrentPeriodEndsOn,
    DateOnly PreviousPeriodStartsOn,
    DateOnly PreviousPeriodEndsOn,
    IReadOnlyCollection<PeriodComparisonItemResponse> Series);

public sealed record IncomeVsExpensePointResponse(
    string Label,
    MoneyResponse Income,
    MoneyResponse Expense,
    MoneyResponse Net);

public sealed record GetIncomeVsExpenseResponse(IReadOnlyCollection<IncomeVsExpensePointResponse> Series);

public sealed record VarianceHistoryPointResponse(
    string Label,
    MoneyResponse Planned,
    MoneyResponse Actual,
    MoneyResponse Variance);

public sealed record GetVarianceHistoryResponse(IReadOnlyCollection<VarianceHistoryPointResponse> Series);

public sealed record TopTransactionResponse(
    Guid TransactionId,
    DateOnly OccurredOn,
    Guid? CategoryId,
    string? CategoryName,
    MoneyResponse Amount,
    string Kind,
    string? Note);

public sealed record GetTopTransactionsResponse(IReadOnlyCollection<TopTransactionResponse> Transactions);
