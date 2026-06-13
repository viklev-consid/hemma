using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Property.Domain;

public sealed record PropertyActivityEventId(Guid Value) : TypedId<Guid>(Value)
{
    public static PropertyActivityEventId New() => new(Guid.NewGuid());
}
