namespace Hemma.Modules.Economy.Features.NotificationPreferences;

public sealed record EconomyNotificationPreferencesResponse(
    Guid HouseholdId,
    bool BudgetAlertsEnabled,
    bool BillAlertsEnabled,
    bool TrialAlertsEnabled,
    DateTimeOffset UpdatedAt);
