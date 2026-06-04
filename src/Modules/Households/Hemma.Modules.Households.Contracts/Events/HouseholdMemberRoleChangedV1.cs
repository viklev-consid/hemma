namespace Hemma.Modules.Households.Contracts.Events;

public sealed record HouseholdMemberRoleChangedV1(
    Guid HouseholdId,
    Guid UserId,
    string OldRole,
    string NewRole,
    Guid ChangedByUserId,
    Guid EventId);
