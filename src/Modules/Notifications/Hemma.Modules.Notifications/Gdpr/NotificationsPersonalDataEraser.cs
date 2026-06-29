using Hemma.Modules.Notifications.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Notifications.Gdpr;

public sealed class NotificationsPersonalDataEraser(NotificationsDbContext db) : IPersonalDataEraser
{
    public async Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        var logs = await db.NotificationLogs
            .Where(l => l.UserId == user.UserId)
            .ToListAsync(ct);
        var notifications = await db.UserNotifications
            .Where(n => n.RecipientUserId == user.UserId)
            .ToListAsync(ct);
        var preferences = await db.NotificationPreferences
            .Where(p => p.UserId == user.UserId)
            .ToListAsync(ct);

        db.NotificationLogs.RemoveRange(logs);
        db.UserNotifications.RemoveRange(notifications);
        db.NotificationPreferences.RemoveRange(preferences);
        await db.SaveChangesAsync(ct);

        return new ErasureResult(user.UserId, ErasureStrategy.HardDelete, logs.Count + notifications.Count + preferences.Count);
    }
}
