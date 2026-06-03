using Hemma.Modules.Users.Domain;

namespace Hemma.Modules.Users.Security;

public interface IJwtGenerator
{
    string Generate(UserId userId, string email, string displayName, string role, Guid refreshTokenId);
}
