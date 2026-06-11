using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Economy.Domain;

public sealed record TransferId(Guid Value) : TypedId<Guid>(Value)
{
    public static TransferId New() => new(Guid.NewGuid());
}
