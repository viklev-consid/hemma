using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.GetBudgetSummary;

public sealed record BudgetSummaryLineResponse(
    Guid CategoryId,
    MoneyDto Planned,
    MoneyDto Actual,
    decimal PacePercent,
    bool IsOverPace);

public sealed record GetBudgetSummaryResponse(
    Guid BudgetId,
    Guid HouseholdId,
    DateOnly PeriodStartsOn,
    DateOnly PeriodEndsOn,
    decimal ElapsedPercent,
    IReadOnlyCollection<BudgetSummaryLineResponse> Lines);
