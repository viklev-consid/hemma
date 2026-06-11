namespace Hemma.Modules.Economy.Features.CreateBudget;

public sealed record CreateBudgetCommand(Guid HouseholdId, DateOnly AnchorDate);
