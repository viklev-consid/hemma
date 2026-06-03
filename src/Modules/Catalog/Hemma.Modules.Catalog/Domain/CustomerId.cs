using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Catalog.Domain;

public sealed record CustomerId(Guid Value) : TypedId<Guid>(Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
}
