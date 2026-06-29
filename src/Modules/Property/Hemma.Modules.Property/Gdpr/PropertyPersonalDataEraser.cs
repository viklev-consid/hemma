using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hemma.Modules.Property.Gdpr;

public sealed partial class PropertyPersonalDataEraser(
    PropertyDbContext db,
    IBlobStore blobStore,
    ILogger<PropertyPersonalDataEraser> logger) : IPersonalDataEraser
{
    public async Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        int affected;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var taskRows = await db.Set<ProjectTask>()
                .Where(task => task.AssigneeId == user.UserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(task => task.AssigneeId, (Guid?)null),
                    ct);

            var activityRows = await db.ActivityEvents
                .Where(activity => activity.ActorId == user.UserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(activity => activity.ActorId, (Guid?)null),
                ct);

            affected = taskRows + activityRows;
            await transaction.CommitAsync(ct);
        }
        catch (PostgresException ex) when (string.Equals(ex.SqlState, PostgresErrorCodes.UndefinedTable, StringComparison.Ordinal))
        {
            LogErasureSkippedMissingTable(logger, user.UserId, ex);
            affected = 0;
        }

        return new ErasureResult(user.UserId, strategy, affected);
    }

    public async Task<int> EraseHouseholdAsync(Guid householdId, CancellationToken ct)
    {
        try
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

            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var blobRefs = projectBlobs
                .Concat(historyBlobs)
                .DistinctBy(blob => (blob.Container, blob.Key))
                .ToArray();

            var existingBlobRefs = (await db.PendingBlobDeletions
                    .Where(deletion => deletion.HouseholdId == householdId)
                    .Select(deletion => new { deletion.BlobContainer, deletion.BlobKey })
                    .ToArrayAsync(ct))
                .Select(deletion => (deletion.BlobContainer, deletion.BlobKey))
                .ToHashSet();

            var pendingBlobs = blobRefs
                .Where(blob => !existingBlobRefs.Contains((blob.Container, blob.Key)))
                .Select(blob => PropertyBlobDeletion.Create(householdId, blob.Container, blob.Key, DateTimeOffset.UtcNow))
                .ToArray();

            db.PendingBlobDeletions.AddRange(pendingBlobs);

            var activity = await db.ActivityEvents
                .Where(activity => activity.HouseholdId == householdId)
                .ExecuteDeleteAsync(ct);

            var projects = await db.Projects
                .Where(project => project.HouseholdId == householdId)
                .ExecuteDeleteAsync(ct);

            var issues = await db.Issues
                .Where(issue => issue.HouseholdId == householdId)
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

            var tagAssignments = await db.TagAssignments
                .Where(assignment => assignment.HouseholdId == householdId)
                .ExecuteDeleteAsync(ct);

            var tags = await db.Tags
                .Where(tag => tag.HouseholdId == householdId)
                .ExecuteDeleteAsync(ct);

            var areas = await db.Areas
                .Where(area => area.HouseholdId == householdId)
                .ExecuteDeleteAsync(ct);

            await transaction.CommitAsync(ct);

            await ProcessPendingBlobDeletionsAsync(pendingBlobs, ct);

            return activity + projects + issues + history + occurrences + plans + tagAssignments + tags + areas;
        }
        catch (PostgresException ex) when (string.Equals(ex.SqlState, PostgresErrorCodes.UndefinedTable, StringComparison.Ordinal))
        {
            LogHouseholdErasureSkippedMissingTable(logger, householdId, ex);
            return 0;
        }
    }

    public async Task<int> ProcessPendingBlobDeletionsAsync(CancellationToken ct)
    {
        var pending = await db.PendingBlobDeletions
            .OrderBy(deletion => deletion.CreatedAt)
            .Take(100)
            .ToArrayAsync(ct);

        return await ProcessPendingBlobDeletionsAsync(pending, ct);
    }

    private async Task<int> ProcessPendingBlobDeletionsAsync(IReadOnlyList<PropertyBlobDeletion> pending, CancellationToken ct)
    {
        var deleted = 0;
        foreach (var deletion in pending)
        {
            deletion.MarkAttempt(DateTimeOffset.UtcNow);
            try
            {
                await blobStore.DeleteAsync(new BlobRef(deletion.BlobContainer, deletion.BlobKey), ct);
                db.PendingBlobDeletions.Remove(deletion);
                deleted++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogBlobDeleteFailed(logger, deletion.HouseholdId, deletion.BlobContainer, deletion.BlobKey, ex);
            }
        }

        await db.SaveChangesAsync(ct);
        return deleted;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Property erasure for user {UserId} skipped: a Property table is missing. If Property migrations have run on this host, this indicates a real deployment error and personal data was NOT erased.")]
    private static partial void LogErasureSkippedMissingTable(ILogger logger, Guid userId, Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Property household erasure for household {HouseholdId} skipped: a Property table is missing. If Property migrations have run on this host, this indicates a real deployment error and personal data was NOT erased.")]
    private static partial void LogHouseholdErasureSkippedMissingTable(ILogger logger, Guid householdId, Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Property blob deletion failed for household {HouseholdId} and pending blob {BlobContainer}/{BlobKey}. The pending row was retained for retry.")]
    private static partial void LogBlobDeleteFailed(ILogger logger, Guid householdId, string blobContainer, string blobKey, Exception exception);
}
