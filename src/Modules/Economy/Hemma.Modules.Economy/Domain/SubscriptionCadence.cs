using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record SubscriptionCadence
{
    private SubscriptionCadence(string frequency, int interval, int chargeDay)
    {
        Frequency = frequency;
        Interval = interval;
        ChargeDay = chargeDay;
    }

    public string Frequency { get; }
    public int Interval { get; }
    public int ChargeDay { get; }

    public static ErrorOr<SubscriptionCadence> Create(string frequency, int interval, int chargeDay)
    {
        if (!string.Equals(frequency, "Monthly", StringComparison.OrdinalIgnoreCase) ||
            interval < 1 ||
            interval > 24 ||
            chargeDay < 1 ||
            chargeDay > 28)
        {
            return EconomyErrors.SubscriptionCadenceInvalid;
        }

        return new SubscriptionCadence("Monthly", interval, chargeDay);
    }

    public bool ChargesInMonth(DateOnly startsOn, int year, int month)
    {
        var charge = ChargeDate(year, month);
        if (charge < startsOn)
        {
            return false;
        }

        var months = ((year - startsOn.Year) * 12) + month - startsOn.Month;
        return months >= 0 && months % Interval == 0;
    }

    public DateOnly ChargeDate(int year, int month) => new(year, month, ChargeDay);
}
