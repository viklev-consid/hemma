namespace Hemma.Modules.Households.Contracts.Events;

public sealed record HouseholdMemberAddedV1(
    Guid HouseholdId,
    Guid UserId,
    string Role,
    Guid EventId);
