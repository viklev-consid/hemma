using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Notifications.Domain;

public sealed record NotificationLogId(Guid Value) : TypedId<Guid>(Value);
