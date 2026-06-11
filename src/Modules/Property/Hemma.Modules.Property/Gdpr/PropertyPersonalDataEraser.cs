using Hemma.Modules.Property.Persistence;
using Hemma.Modules.Property.Domain;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Gdpr;

public sealed class PropertyPersonalDataEraser(PropertyDbContext db, IBlobStore blobStore) : IPersonalDataEraser
{
    public async Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        var affected = await db.Set<ProjectTask>()
            .Where(task => task.AssigneeId == user.UserId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(task => task.AssigneeId, (Guid?)null),
                ct);

        return new ErasureResult(user.UserId, strategy, affected);
    }

    public async Task<int> EraseHouseholdAsync(Guid householdId, CancellationToken ct)
    {
        var projectBlobs = await db.Projects
            .AsNoTracking()
            .Where(project => project.HouseholdId == householdId)
            .SelectMany(project => project.Attachments)
            .Select(attachment => new BlobRef(attachment.BlobContainer, attachment.BlobKey))
            .ToArrayAsync(ct);

        var historyBlobs = await db.HistoryEntries
            .AsNoTracking()
            .Where(entry => entry.HouseholdId == householdId)
            .SelectMany(entry => entry.Photos)
            .Select(photo => new BlobRef(photo.BlobContainer, photo.BlobKey))
            .ToArrayAsync(ct);

        var projects = await db.Projects
            .Where(project => project.HouseholdId == householdId)
            .ExecuteDeleteAsync(ct);

        var history = await db.HistoryEntries
            .Where(entry => entry.HouseholdId == householdId)
            .ExecuteDeleteAsync(ct);

        var occurrences = await db.MaintenanceOccurrences
            .Where(o => o.HouseholdId == householdId)
            .ExecuteDeleteAsync(ct);

        var plans = await db.MaintenancePlans
            .Where(plan => plan.HouseholdId == householdId)
            .ExecuteDeleteAsync(ct);

        foreach (var blob in projectBlobs.Concat(historyBlobs))
        {
            await blobStore.DeleteAsync(blob, ct);
        }

        return projects + history + occurrences + plans;
    }
}
