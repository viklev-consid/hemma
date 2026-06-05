namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record CreateSubscriptionCommand(
    Guid HouseholdId,
    string Name,
    string CadenceFrequency,
    int CadenceInterval,
    int ChargeDay,
    decimal ExpectedAmount,
    string ExpectedCurrency,
    string LifecycleState,
    DateOnly? TrialEndsOn,
    Guid? AccountId,
    DateOnly StartsOn);
