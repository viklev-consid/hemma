using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Gdpr;

public sealed class PropertyPersonalDataEraser(PropertyDbContext db) : IPersonalDataEraser
{
    public Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        // Phase 0 has no user-owned Property rows. When ProjectTask.AssigneeId scrubbing lands, do it here.
        return Task.FromResult(new ErasureResult(user.UserId, strategy, RecordsAffected: 0));
    }

    public async Task<int> EraseHouseholdAsync(Guid householdId, CancellationToken ct)
    {
        // Household deletion cascades to all maintenance aggregates the household owns (no blobs).
        // Project/blob cascade is wired in Phase 4 alongside the Logbook blob ownership work.
        var occurrences = await db.MaintenanceOccurrences
            .Where(o => o.HouseholdId == householdId)
            .ExecuteDeleteAsync(ct);

        var plans = await db.MaintenancePlans
            .Where(plan => plan.HouseholdId == householdId)
            .ExecuteDeleteAsync(ct);

        return occurrences + plans;
    }
}
