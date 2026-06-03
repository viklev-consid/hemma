using Hemma.Modules.Users.Domain;

namespace Hemma.Modules.Users.Security;

public interface ITwoFactorRequirementEvaluator
{
    Task<bool> IsRequiredAsync(User user, CancellationToken ct);
}
