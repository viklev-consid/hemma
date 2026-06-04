namespace Hemma.Modules.Economy.Features.UpsertBudgetLine;

public sealed record UpsertBudgetLineCommand(Guid HouseholdId, Guid BudgetId, Guid CategoryId, decimal Amount, string Currency);
