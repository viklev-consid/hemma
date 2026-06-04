using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.Features.Contracts;

public sealed record BudgetLineResponse(Guid BudgetLineId, Guid CategoryId, MoneyResponse Amount);

public sealed record BudgetResponse(
    Guid BudgetId,
    Guid HouseholdId,
    DateOnly PeriodStartsOn,
    DateOnly PeriodEndsOn,
    DateOnly PaceStartsOn,
    IReadOnlyCollection<BudgetLineResponse> Lines)
{
    public static BudgetResponse From(Budget budget, DateOnly paceStartsOn) =>
        new(
            budget.Id.Value,
            budget.HouseholdId,
            budget.PeriodStartsOn,
            budget.PeriodEndsOn,
            paceStartsOn,
            budget.Lines
                .OrderBy(line => line.CategoryId.Value)
                .Select(line => new BudgetLineResponse(line.Id.Value, line.CategoryId.Value, MoneyResponse.From(line.Amount)))
                .ToArray());
}
