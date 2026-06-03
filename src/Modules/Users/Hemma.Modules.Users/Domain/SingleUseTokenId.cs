using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record SingleUseTokenId(Guid Value) : TypedId<Guid>(Value)
{
    public static SingleUseTokenId New() => new(Guid.NewGuid());
}
