using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.UpsertBudgetLine;

public sealed record UpsertBudgetLineRequest(Guid HouseholdId, Guid BudgetId, Guid CategoryId, MoneyRequest Amount);
