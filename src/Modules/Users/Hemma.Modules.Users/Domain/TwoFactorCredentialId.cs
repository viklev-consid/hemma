using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record TwoFactorCredentialId(Guid Value) : TypedId<Guid>(Value)
{
    public static TwoFactorCredentialId New() => new(Guid.NewGuid());
}
