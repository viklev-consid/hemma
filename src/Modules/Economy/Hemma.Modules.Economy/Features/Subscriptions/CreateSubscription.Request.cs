using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record CreateSubscriptionRequest(
    Guid HouseholdId,
    string Name,
    string CadenceFrequency,
    int CadenceInterval,
    int ChargeDay,
    MoneyDto ExpectedAmount,
    string LifecycleState,
    DateOnly? TrialEndsOn,
    Guid? AccountId,
    DateOnly StartsOn);
