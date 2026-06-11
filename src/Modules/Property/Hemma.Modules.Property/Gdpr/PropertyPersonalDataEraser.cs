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
            affected = await db.Set<ProjectTask>()
                .Where(task => task.AssigneeId == user.UserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(task => task.AssigneeId, (Guid?)null),
                    ct);
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
        catch (PostgresException ex) when (string.Equals(ex.SqlState, PostgresErrorCodes.UndefinedTable, StringComparison.Ordinal))
        {
            LogHouseholdErasureSkippedMissingTable(logger, householdId, ex);
            return 0;
        }
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
}
