using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Economy.Domain;

public sealed record AccountId(Guid Value) : TypedId<Guid>(Value)
{
    public static AccountId New() => new(Guid.NewGuid());
}
