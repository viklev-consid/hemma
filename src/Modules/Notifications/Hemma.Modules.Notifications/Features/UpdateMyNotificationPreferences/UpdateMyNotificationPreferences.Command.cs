using Hemma.Modules.Notifications.Contracts.Dtos;

namespace Hemma.Modules.Notifications.Features.UpdateMyNotificationPreferences;

public sealed record UpdateMyNotificationPreferencesCommand(
    Guid UserId,
    IReadOnlyList<UpdateMyNotificationPreference> Preferences);

public sealed record UpdateMyNotificationPreference(
    NotificationCategory Category,
    bool BellEnabled,
    bool EmailEnabled);
