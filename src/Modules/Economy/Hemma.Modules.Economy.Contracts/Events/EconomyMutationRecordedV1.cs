namespace Hemma.Modules.Economy.Contracts.Events;

public sealed record EconomyMutationRecordedV1(
    Guid HouseholdId,
    string Action,
    string ResourceType,
    Guid ResourceId,
    Guid? ActorId,
    Guid EventId);
