using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.Gdpr;

public sealed class PropertyPersonalDataEraser(PropertyDbContext db) : IPersonalDataEraser
{
    public Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        // Phase 0 has no user-owned Property rows. When ProjectTask.AssigneeId lands, scrub it here.
        return Task.FromResult(new ErasureResult(user.UserId, strategy, RecordsAffected: 0));
    }

    public Task<int> EraseHouseholdAsync(Guid householdId, CancellationToken ct)
    {
        // Phase 0 has no household-owned rows or blobs. Later phases add the real cascade here.
        return db.SaveChangesAsync(ct);
    }
}
