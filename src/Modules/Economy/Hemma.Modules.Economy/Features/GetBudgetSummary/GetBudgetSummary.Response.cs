using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.GetBudgetSummary;

public sealed record BudgetSummaryLineResponse(
    Guid CategoryId,
    MoneyResponse Planned,
    MoneyResponse Actual,
    decimal PacePercent,
    bool IsOverPace);

public sealed record GetBudgetSummaryResponse(
    Guid BudgetId,
    Guid HouseholdId,
    DateOnly PeriodStartsOn,
    DateOnly PeriodEndsOn,
    decimal ElapsedPercent,
    IReadOnlyCollection<BudgetSummaryLineResponse> Lines);
