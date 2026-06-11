namespace Hemma.Modules.Economy.Domain;

public sealed record BudgetPeriod(DateOnly StartsOn, DateOnly EndsOn)
{
    public static BudgetPeriod Containing(DateOnly date, int cycleStartDay)
    {
        var start = new DateOnly(date.Year, date.Month, cycleStartDay);
        if (date.Day < cycleStartDay)
        {
            start = start.AddMonths(-1);
        }

        return new BudgetPeriod(start, start.AddMonths(1).AddDays(-1));
    }

    public BudgetPeriod Previous() => new(StartsOn.AddMonths(-1), StartsOn.AddDays(-1));
}
