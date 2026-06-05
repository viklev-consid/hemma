using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record RecurringBillCadence
{
    private RecurringBillCadence(string Frequency, int Interval, int DayOfMonth)
    {
        this.Frequency = Frequency;
        this.Interval = Interval;
        this.DayOfMonth = DayOfMonth;
    }

    public string Frequency { get; }
    public int Interval { get; }
    public int DayOfMonth { get; }

    public static ErrorOr<RecurringBillCadence> Create(string frequency, int interval, int dayOfMonth)
    {
        if (!string.Equals(frequency, "Monthly", StringComparison.OrdinalIgnoreCase) ||
            interval < 1 ||
            interval > 24 ||
            dayOfMonth < 1 ||
            dayOfMonth > 28)
        {
            return EconomyErrors.RecurringBillCadenceInvalid;
        }

        return new RecurringBillCadence("Monthly", interval, dayOfMonth);
    }

    public DateOnly NextDueOn(DateOnly from)
    {
        var candidate = new DateOnly(from.Year, from.Month, DayOfMonth);
        if (candidate < from)
        {
            candidate = candidate.AddMonths(Interval);
        }

        return candidate;
    }

    public DateOnly NextAfter(DateOnly occurrence) => occurrence.AddMonths(Interval);
}
