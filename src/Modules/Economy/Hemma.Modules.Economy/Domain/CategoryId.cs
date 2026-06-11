using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Economy.Domain;

public sealed record CategoryId(Guid Value) : TypedId<Guid>(Value)
{
    public static CategoryId New() => new(Guid.NewGuid());
}
