namespace Hemma.Modules.Economy.Features.NotificationPreferences;

public sealed record UpdateEconomyNotificationPreferencesCommand(
    Guid HouseholdId,
    bool BudgetAlertsEnabled,
    bool BillAlertsEnabled,
    bool TrialAlertsEnabled);
