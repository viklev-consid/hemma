namespace Hemma.Modules.Users.Contracts.Events;

public sealed record UserProfileUpdatedV1(Guid UserId, string OldDisplayName, string NewDisplayName, Guid EventId);
