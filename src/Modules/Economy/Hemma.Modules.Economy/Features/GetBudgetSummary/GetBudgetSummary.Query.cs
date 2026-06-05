namespace Hemma.Modules.Economy.Features.GetBudgetSummary;

public sealed record GetBudgetSummaryQuery(Guid HouseholdId, DateOnly AnchorDate);
