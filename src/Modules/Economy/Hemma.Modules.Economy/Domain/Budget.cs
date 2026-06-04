using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class Budget : AggregateRoot<BudgetId>
{
    private readonly List<BudgetLine> lines = [];

    private Budget(BudgetId id, Guid householdId, BudgetPeriod period) : base(id)
    {
        HouseholdId = householdId;
        PeriodStartsOn = period.StartsOn;
        PeriodEndsOn = period.EndsOn;
    }

    private Budget() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public DateOnly PeriodStartsOn { get; private set; }
    public DateOnly PeriodEndsOn { get; private set; }
    public IReadOnlyCollection<BudgetLine> Lines => lines;

    public static Budget Create(Guid householdId, BudgetPeriod period) => new(BudgetId.New(), householdId, period);

    public ErrorOr<BudgetLine> UpsertLine(Category category, Money amount)
    {
        if (!category.Budgetable)
        {
            return EconomyErrors.BudgetLineNotAllowed;
        }

        var existing = lines.FirstOrDefault(line => line.CategoryId == category.Id);
        if (existing is not null)
        {
            existing.UpdateAmount(amount);
            return existing;
        }

        var line = BudgetLine.Create(Id, category.Id, amount);
        lines.Add(line);
        return line;
    }
}
