namespace Hemma.Modules.Economy.Contracts.Events;

public sealed record TrialRenewalDueV1(
    Guid SubscriptionId,
    Guid HouseholdId,
    DateOnly TrialEndsOn,
    Guid EventId);
