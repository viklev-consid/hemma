using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class EconomyNotificationPreferences : AggregateRoot<EconomyNotificationPreferencesId>
{
    private EconomyNotificationPreferences(
        EconomyNotificationPreferencesId id,
        Guid householdId,
        bool budgetAlertsEnabled,
        bool billAlertsEnabled,
        bool trialAlertsEnabled,
        DateTimeOffset updatedAt) : base(id)
    {
        HouseholdId = householdId;
        BudgetAlertsEnabled = budgetAlertsEnabled;
        BillAlertsEnabled = billAlertsEnabled;
        TrialAlertsEnabled = trialAlertsEnabled;
        UpdatedAt = updatedAt;
    }

    private EconomyNotificationPreferences() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public bool BudgetAlertsEnabled { get; private set; }
    public bool BillAlertsEnabled { get; private set; }
    public bool TrialAlertsEnabled { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static EconomyNotificationPreferences CreateDefault(Guid householdId, DateTimeOffset updatedAt) =>
        new(new EconomyNotificationPreferencesId(Guid.NewGuid()), householdId, true, true, true, updatedAt);

    public void Update(
        bool budgetAlertsEnabled,
        bool billAlertsEnabled,
        bool trialAlertsEnabled,
        DateTimeOffset updatedAt)
    {
        BudgetAlertsEnabled = budgetAlertsEnabled;
        BillAlertsEnabled = billAlertsEnabled;
        TrialAlertsEnabled = trialAlertsEnabled;
        UpdatedAt = updatedAt;
    }
}
