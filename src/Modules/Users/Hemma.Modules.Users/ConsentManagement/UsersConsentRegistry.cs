using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Users.ConsentManagement;

public sealed class UsersConsentRegistry(UsersDbContext db, IClock clock) : IConsentRegistry
{
    public async Task<bool> HasConsentedAsync(Guid userId, string consentKey, CancellationToken ct = default)
    {
        var latest = await db.Consents
            .Where(c => c.UserId == userId && c.ConsentKey == consentKey)
            .OrderByDescending(c => c.RecordedAt)
            .FirstOrDefaultAsync(ct);

        return latest?.Granted ?? false;
    }

    public async Task GrantAsync(Guid userId, string consentKey, CancellationToken ct = default)
    {
        var consent = Consent.Grant(userId, consentKey, clock.UtcNow);
        db.Consents.Add(consent);
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(Guid userId, string consentKey, CancellationToken ct = default)
    {
        var consent = Consent.Revoke(userId, consentKey, clock.UtcNow);
        db.Consents.Add(consent);
        await db.SaveChangesAsync(ct);
    }
}
