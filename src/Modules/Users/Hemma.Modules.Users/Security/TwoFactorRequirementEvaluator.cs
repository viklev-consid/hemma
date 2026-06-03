using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Persistence;

namespace Hemma.Modules.Users.Security;

internal sealed class TwoFactorRequirementEvaluator(UsersDbContext db) : ITwoFactorRequirementEvaluator
{
    public async Task<bool> IsRequiredAsync(User user, CancellationToken ct) =>
        await db.TwoFactorCredentials
            .Where(c => c.UserId == user.Id && c.Method == TwoFactorMethod.Totp)
            .WhereActive()
            .AnyAsync(ct);
}
