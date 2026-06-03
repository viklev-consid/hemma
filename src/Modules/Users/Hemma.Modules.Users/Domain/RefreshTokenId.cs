using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record RefreshTokenId(Guid Value) : TypedId<Guid>(Value)
{
    public static RefreshTokenId New() => new(Guid.NewGuid());
}
