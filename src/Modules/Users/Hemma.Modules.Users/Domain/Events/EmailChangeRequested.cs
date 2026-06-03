using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Users.Domain.Events;

/// <summary>Internal domain event raised when a user requests an email change (before confirmation).</summary>
public sealed record EmailChangeRequested(UserId UserId, string NewEmail) : DomainEvent;
