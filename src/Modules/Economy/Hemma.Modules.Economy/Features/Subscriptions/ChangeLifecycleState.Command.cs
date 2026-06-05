namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record ChangeLifecycleStateCommand(Guid HouseholdId, Guid SubscriptionId, string LifecycleState, DateOnly? TrialEndsOn);
