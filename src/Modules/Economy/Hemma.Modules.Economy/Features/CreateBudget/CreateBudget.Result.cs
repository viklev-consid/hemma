using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.CreateBudget;

public sealed record CreateBudgetResult(BudgetResponse Budget, bool Created);
