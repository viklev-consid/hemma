using ErrorOr;
using Hemma.Modules.Notifications.Domain;
using Hemma.Modules.Notifications.Errors;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Notifications.Policies;
using Hemma.Modules.Notifications.Streaming;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Notifications.Features.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandler(
    NotificationsDbContext db,
    IClock clock,
    NotificationRetentionPolicy retentionPolicy,
    INotificationStreamPublisher streamPublisher)
{
    public async Task<ErrorOr<Success>> Handle(MarkNotificationAsReadCommand command, CancellationToken ct)
    {
        var notification = await db.UserNotifications
            .SingleOrDefaultAsync(n => n.Id == new UserNotificationId(command.NotificationId)
                                       && n.RecipientUserId == command.UserId
                                       && n.ArchivedAt == null, ct);

        if (notification is null)
        {
            return NotificationsErrors.NotificationNotFound;
        }

        if (notification.IsRead)
        {
            return Result.Success;
        }

        var readAt = clock.UtcNow;
        notification.MarkRead(readAt, retentionPolicy.GetReadRetentionUntil(notification.Category, readAt));
        await db.SaveChangesAsync(ct);

        await streamPublisher.PublishAsync(
            command.UserId,
            new NotificationStreamEvent("notification.read", $$"""
            {"id":"{{command.NotificationId}}","unreadCountChanged":true}
            """),
            ct);

        return Result.Success;
    }
}
