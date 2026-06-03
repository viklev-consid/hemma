using Hemma.Modules.Notifications.Contracts.Dtos;

namespace Hemma.Modules.Notifications.Features.GetMyNotificationPreferences;

public sealed record GetMyNotificationPreferencesResponse(IReadOnlyList<MyNotificationPreferenceResponse> Preferences);

public sealed record MyNotificationPreferenceResponse(
    NotificationCategory Category,
    bool BellEnabled,
    bool EmailEnabled,
    bool IsLocked);
