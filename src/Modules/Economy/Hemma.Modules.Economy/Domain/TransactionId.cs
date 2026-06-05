using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Economy.Domain;

public sealed record TransactionId(Guid Value) : TypedId<Guid>(Value)
{
    public static TransactionId New() => new(Guid.NewGuid());
}
