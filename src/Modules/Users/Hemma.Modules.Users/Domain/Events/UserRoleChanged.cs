using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Users.Domain.Events;

internal sealed record UserRoleChanged(
    UserId UserId,
    string OldRole,
    string NewRole,
    UserId ChangedBy) : DomainEvent;
