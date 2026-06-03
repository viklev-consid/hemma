using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record ConsentId(Guid Value) : TypedId<Guid>(Value)
{
    public static ConsentId New() => new(Guid.NewGuid());
}
