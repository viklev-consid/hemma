namespace Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;

public sealed record CopyBudgetFromPreviousPeriodCommand(Guid HouseholdId, DateOnly AnchorDate);
