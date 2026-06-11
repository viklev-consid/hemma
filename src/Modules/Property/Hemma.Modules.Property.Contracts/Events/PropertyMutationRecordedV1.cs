namespace Hemma.Modules.Property.Contracts.Events;

public sealed record PropertyMutationRecordedV1(
    Guid HouseholdId,
    string Action,
    string ResourceType,
    Guid ResourceId,
    Guid? ActorId,
    Guid EventId);
