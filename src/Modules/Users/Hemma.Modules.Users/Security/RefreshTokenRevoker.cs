using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Users.Security;

public sealed class RefreshTokenRevoker(UsersDbContext db, IClock clock) : IRefreshTokenRevoker
{
    public async Task RevokeAllForUserAsync(UserId userId, CancellationToken ct, RefreshTokenId? except = null)
    {
        var query = db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null);

        if (except is not null)
        {
            query = query.Where(t => t.Id != except);
        }

        await query.ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, clock.UtcNow), ct);
    }
}
