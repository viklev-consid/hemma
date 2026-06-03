using Hemma.Modules.Users.Domain;

namespace Hemma.Modules.Users.Security;

public interface ITwoFactorChallengeIssuer
{
    (PendingTwoFactorChallenge challenge, string rawValue) Issue(UserId userId, string? ipAddress);
}
