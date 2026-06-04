namespace Hemma.Modules.Households.Contracts.Events;

public sealed record HouseholdDeletedV1(
    Guid HouseholdId,
    Guid DeletedByUserId,
    Guid EventId);
