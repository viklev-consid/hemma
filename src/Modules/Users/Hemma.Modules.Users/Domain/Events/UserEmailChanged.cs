using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Users.Domain.Events;

internal sealed record UserEmailChanged(UserId UserId, string OldEmail, string NewEmail) : DomainEvent;
