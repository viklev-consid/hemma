namespace Hemma.Modules.Economy.Features.CreateBudget;

public sealed record CreateBudgetRequest(Guid HouseholdId, DateOnly AnchorDate);
