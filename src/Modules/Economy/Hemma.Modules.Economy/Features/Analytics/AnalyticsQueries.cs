using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.Features.Analytics;

public sealed record GetCategoryTrendQuery(Guid HouseholdId, DateOnly From, DateOnly To);

public sealed record GetSpendBreakdownQuery(Guid HouseholdId, DateOnly From, DateOnly To);

public sealed record GetPeriodComparisonQuery(Guid HouseholdId, DateOnly AnchorDate);

public sealed record GetIncomeVsExpenseQuery(Guid HouseholdId, DateOnly From, DateOnly To);

public sealed record GetVarianceHistoryQuery(Guid HouseholdId, DateOnly From, DateOnly To);

public sealed record GetTopTransactionsQuery(Guid HouseholdId, DateOnly From, DateOnly To, CategoryId? CategoryId, int Limit);
