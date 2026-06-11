using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.Gdpr;

public sealed class PropertyPersonalDataEraser(PropertyDbContext db) : IPersonalDataEraser
{
    public Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        return Task.FromResult(new ErasureResult(user.UserId, strategy, RecordsAffected: 0));
    }

    public Task<int> EraseHouseholdAsync(Guid householdId, CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }
}
