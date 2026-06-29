namespace Hemma.Modules.Property.Contracts.Events;

public sealed record ProjectDeletedV1(Guid HouseholdId, Guid ProjectId, Guid EventId);
