using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record RecoveryCodeId(Guid Value) : TypedId<Guid>(Value)
{
    public static RecoveryCodeId New() => new(Guid.NewGuid());
}
