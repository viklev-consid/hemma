using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class BudgetLine : Entity<BudgetLineId>
{
    private BudgetLine(BudgetLineId id, BudgetId budgetId, CategoryId categoryId, Money amount) : base(id)
    {
        BudgetId = budgetId;
        CategoryId = categoryId;
        Amount = amount;
    }

    private BudgetLine() : base(default!) { }

    public BudgetId BudgetId { get; private set; } = null!;
    public CategoryId CategoryId { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;

    public static BudgetLine Create(BudgetId budgetId, CategoryId categoryId, Money amount) =>
        new(BudgetLineId.New(), budgetId, categoryId, amount);

    public void UpdateAmount(Money amount)
    {
        Amount = amount;
    }
}
