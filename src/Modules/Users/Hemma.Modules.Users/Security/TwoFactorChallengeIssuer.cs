using Microsoft.Extensions.Options;
using Hemma.Modules.Users.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Users.Security;

internal sealed class TwoFactorChallengeIssuer(
    IOptions<UsersOptions> options,
    IClock clock) : ITwoFactorChallengeIssuer
{
    public (PendingTwoFactorChallenge challenge, string rawValue) Issue(UserId userId, string? ipAddress) =>
        PendingTwoFactorChallenge.Create(userId, options.Value.TwoFactorChallengeLifetime, clock, ipAddress);
}
