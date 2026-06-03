using Hemma.Shared.Kernel.Identifiers;

namespace Hemma.Modules.Users.Domain;

public sealed record PendingTwoFactorChallengeId(Guid Value) : TypedId<Guid>(Value)
{
    public static PendingTwoFactorChallengeId New() => new(Guid.NewGuid());
}
