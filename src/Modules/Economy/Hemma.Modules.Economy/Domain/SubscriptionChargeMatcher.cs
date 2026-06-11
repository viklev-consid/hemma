namespace Hemma.Modules.Economy.Domain;

public static class SubscriptionChargeMatcher
{
    private const int maxDayDelta = 3;
    private const decimal minAmountTolerance = 5m;
    private const decimal amountToleranceRate = 0.10m;

    public static SubscriptionChargeMatch? Match(
        Subscription subscription,
        DateOnly occurredOn,
        decimal amount,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(description) ||
            !description.Contains(subscription.Name, StringComparison.OrdinalIgnoreCase) ||
            !subscription.Cadence.ChargesInMonth(subscription.StartsOn, occurredOn.Year, occurredOn.Month))
        {
            return null;
        }

        var dayDelta = Math.Abs(subscription.Cadence.ChargeDay - occurredOn.Day);
        var amountDelta = Math.Abs(subscription.ExpectedAmount.Amount - amount);
        var amountTolerance = Math.Max(minAmountTolerance, subscription.ExpectedAmount.Amount * amountToleranceRate);

        return dayDelta <= maxDayDelta && amountDelta <= amountTolerance
            ? new SubscriptionChargeMatch(dayDelta, amountDelta)
            : null;
    }
}
