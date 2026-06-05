using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record CreateSubscriptionRequest(
    Guid HouseholdId,
    string Name,
    string CadenceFrequency,
    int CadenceInterval,
    int ChargeDay,
    MoneyRequest ExpectedAmount,
    string LifecycleState,
    DateOnly? TrialEndsOn,
    Guid? AccountId,
    DateOnly StartsOn);
