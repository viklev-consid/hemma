using ErrorOr;
using Hemma.Modules.Notifications.Mapping;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Notifications.Policies;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Notifications.Features.GetMyNotificationPreferences;

public sealed class GetMyNotificationPreferencesHandler(NotificationsDbContext db)
{
    public async Task<ErrorOr<GetMyNotificationPreferencesResponse>> Handle(
        GetMyNotificationPreferencesQuery query,
        CancellationToken ct)
    {
        var preferences = await db.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.UserId == query.UserId)
            .ToDictionaryAsync(p => p.Category, ct);

        return new GetMyNotificationPreferencesResponse(
            NotificationPreferenceDefaults.All.Select(defaults =>
            {
                preferences.TryGetValue(defaults.Category, out var stored);
                return new MyNotificationPreferenceResponse(
                    defaults.Category.ToContract(),
                    stored?.BellEnabled ?? defaults.BellEnabled,
                    stored?.EmailEnabled ?? defaults.EmailEnabled,
                    defaults.IsLocked);
            }).ToList());
    }
}
