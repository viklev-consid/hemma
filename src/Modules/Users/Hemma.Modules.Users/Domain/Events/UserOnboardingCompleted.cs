using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Users.Domain.Events;

internal sealed record UserOnboardingCompleted(UserId UserId) : DomainEvent;
