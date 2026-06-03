using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Users.Domain.Events;

internal sealed record UserProfileUpdated(UserId UserId, string OldDisplayName, string NewDisplayName) : DomainEvent;
