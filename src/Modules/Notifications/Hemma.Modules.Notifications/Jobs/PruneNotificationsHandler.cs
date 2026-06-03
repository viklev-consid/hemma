using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Notifications.Jobs;

public sealed record PruneNotifications;

public sealed class PruneNotificationsHandler(
    NotificationsDbContext db,
    IClock clock)
{
    public async Task Handle(PruneNotifications _, CancellationToken ct)
    {
        var now = clock.UtcNow;

        await db.UserNotifications
            .Where(n => n.RetentionUntil < now)
            .ExecuteDeleteAsync(ct);
    }
}
