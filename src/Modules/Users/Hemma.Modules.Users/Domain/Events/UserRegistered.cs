using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Users.Domain.Events;

internal sealed record UserRegistered(UserId UserId, string Email, string DisplayName) : DomainEvent;
