using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.NotificationPreferences;

public sealed class EconomyNotificationPreferencesHandler(
    EconomyDbContext db,
    IClock clock,
    EconomyAuditPublisher audit)
{
    public async Task<EconomyNotificationPreferencesResponse> Handle(
        GetEconomyNotificationPreferencesQuery query,
        CancellationToken ct)
    {
        var preferences = await GetOrCreateAsync(query.HouseholdId, ct);
        return ToResponse(preferences);
    }

    public async Task<ErrorOr<EconomyNotificationPreferencesResponse>> Handle(
        UpdateEconomyNotificationPreferencesCommand cmd,
        CancellationToken ct)
    {
        var preferences = await GetOrCreateAsync(cmd.HouseholdId, ct);
        preferences.Update(
            cmd.BudgetAlertsEnabled,
            cmd.BillAlertsEnabled,
            cmd.TrialAlertsEnabled,
            clock.UtcNow);

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(
            preferences.HouseholdId,
            "economy.notification_preferences.updated",
            "EconomyNotificationPreferences",
            preferences.Id.Value,
            null,
            ct);

        return ToResponse(preferences);
    }

    private async Task<EconomyNotificationPreferences> GetOrCreateAsync(Guid householdId, CancellationToken ct)
    {
        var preferences = await db.NotificationPreferences
            .SingleOrDefaultAsync(value => value.HouseholdId == householdId, ct);
        if (preferences is not null)
        {
            return preferences;
        }

        preferences = EconomyNotificationPreferences.CreateDefault(householdId, clock.UtcNow);
        db.NotificationPreferences.Add(preferences);
        await db.SaveChangesAsync(ct);
        return preferences;
    }

    private static EconomyNotificationPreferencesResponse ToResponse(EconomyNotificationPreferences preferences) =>
        new(
            preferences.HouseholdId,
            preferences.BudgetAlertsEnabled,
            preferences.BillAlertsEnabled,
            preferences.TrialAlertsEnabled,
            preferences.UpdatedAt);
}
