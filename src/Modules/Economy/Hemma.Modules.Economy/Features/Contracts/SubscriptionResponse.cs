using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.Features.Contracts;

public sealed record SubscriptionResponse(
    Guid SubscriptionId,
    Guid HouseholdId,
    string Name,
    string CadenceFrequency,
    int CadenceInterval,
    int ChargeDay,
    MoneyResponse ExpectedAmount,
    string LifecycleState,
    DateOnly? TrialEndsOn,
    Guid? AccountId,
    DateOnly StartsOn)
{
    public static SubscriptionResponse From(Subscription subscription) =>
        new(
            subscription.Id.Value,
            subscription.HouseholdId,
            subscription.Name,
            subscription.Cadence.Frequency,
            subscription.Cadence.Interval,
            subscription.Cadence.ChargeDay,
            MoneyResponse.From(subscription.ExpectedAmount),
            subscription.LifecycleState.Name,
            subscription.TrialEndsOn,
            subscription.AccountId?.Value,
            subscription.StartsOn);
}
