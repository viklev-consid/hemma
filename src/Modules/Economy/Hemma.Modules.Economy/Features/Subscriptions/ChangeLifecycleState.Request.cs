namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record ChangeLifecycleStateRequest(Guid HouseholdId, string LifecycleState, DateOnly? TrialEndsOn);
