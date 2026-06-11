using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.CreateBudget;

public sealed record CreateBudgetResult(BudgetResponse Budget, bool Created);
