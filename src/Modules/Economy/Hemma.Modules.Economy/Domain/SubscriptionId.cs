using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Economy.Domain;

public sealed record SubscriptionId(Guid Value) : TypedId<Guid>(Value)
{
    public static SubscriptionId New() => new(Guid.NewGuid());
}
