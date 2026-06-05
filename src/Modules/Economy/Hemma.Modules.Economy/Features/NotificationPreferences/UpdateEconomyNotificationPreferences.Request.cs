namespace Hemma.Modules.Economy.Features.NotificationPreferences;

public sealed record UpdateEconomyNotificationPreferencesRequest(
    Guid HouseholdId,
    bool BudgetAlertsEnabled,
    bool BillAlertsEnabled,
    bool TrialAlertsEnabled);
