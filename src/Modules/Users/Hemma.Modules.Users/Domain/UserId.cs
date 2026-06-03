using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record UserId(Guid Value) : TypedId<Guid>(Value)
{
    public static UserId New() => new(Guid.NewGuid());
}
