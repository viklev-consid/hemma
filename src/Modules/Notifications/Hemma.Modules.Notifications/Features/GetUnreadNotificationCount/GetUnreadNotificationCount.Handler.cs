using ErrorOr;
using Hemma.Modules.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Notifications.Features.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountHandler(NotificationsDbContext db)
{
    public async Task<ErrorOr<GetUnreadNotificationCountResponse>> Handle(GetUnreadNotificationCountQuery query, CancellationToken ct)
    {
        var count = await db.UserNotifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == query.UserId && n.ReadAt == null && n.ArchivedAt == null, ct);

        return new GetUnreadNotificationCountResponse(count);
    }
}
