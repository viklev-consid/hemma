namespace Hemma.Modules.Households.Contracts.Events;

public sealed record HouseholdMemberRemovedV1(
    Guid HouseholdId,
    Guid UserId,
    Guid RemovedByUserId,
    Guid EventId);
