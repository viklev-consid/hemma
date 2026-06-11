namespace Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;

public sealed record CopyBudgetFromPreviousPeriodRequest(Guid HouseholdId, DateOnly AnchorDate);
