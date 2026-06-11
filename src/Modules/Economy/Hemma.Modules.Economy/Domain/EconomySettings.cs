using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class EconomySettings : AggregateRoot<EconomySettingsId>
{
    private EconomySettings(
        EconomySettingsId id,
        Guid householdId,
        int cycleStartDay,
        string defaultCurrency,
        DateOnly createdOn) : base(id)
    {
        HouseholdId = householdId;
        CycleStartDay = cycleStartDay;
        DefaultCurrency = defaultCurrency;
        CreatedOn = createdOn;
    }

    private EconomySettings() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public int CycleStartDay { get; private set; }
    public string DefaultCurrency { get; private set; } = null!;
    public DateOnly CreatedOn { get; private set; }

    public static ErrorOr<EconomySettings> Create(Guid householdId, int cycleStartDay, string defaultCurrency, DateOnly createdOn)
    {
        var day = ValidateCycleStartDay(cycleStartDay);
        if (day.IsError)
        {
            return day.Errors;
        }

        var currency = Money.Create(0, defaultCurrency);
        if (currency.IsError)
        {
            return currency.Errors;
        }

        return new EconomySettings(EconomySettingsId.New(), householdId, cycleStartDay, currency.Value.Currency, createdOn);
    }

    public ErrorOr<Success> UpdateCycleStartDay(int cycleStartDay)
    {
        var day = ValidateCycleStartDay(cycleStartDay);
        if (day.IsError)
        {
            return day.Errors;
        }

        CycleStartDay = cycleStartDay;
        return Result.Success;
    }

    public BudgetPeriod GetPeriodContaining(DateOnly date) => BudgetPeriod.Containing(date, CycleStartDay);

    private static ErrorOr<Success> ValidateCycleStartDay(int cycleStartDay) =>
        cycleStartDay is >= 1 and <= 28
            ? Result.Success
            : EconomyErrors.CycleStartDayInvalid;
}
