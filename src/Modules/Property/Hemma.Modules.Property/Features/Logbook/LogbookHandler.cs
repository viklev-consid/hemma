using ErrorOr;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Contracts;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.Logbook;

public sealed class LogbookHandler(PropertyDbContext db, IBlobStore blobStore, PropertyAuditPublisher audit)
{
    public async Task<ErrorOr<HistoryEntryResponse>> Handle(CreateHistoryEntryCommand cmd, CancellationToken ct)
    {
        var type = ParseType(cmd.Type);
        if (type is null)
        {
            return PropertyErrors.HistoryEntryTypeInvalid;
        }

        var cost = ToMoney(cmd.Cost);
        if (cost.IsError)
        {
            return cost.Errors;
        }

        var entry = HistoryEntry.Create(
            cmd.HouseholdId,
            cmd.Date,
            cmd.Title,
            cmd.Area,
            cost.Value.Value,
            type.Value,
            cmd.SourceProjectId,
            cmd.SourceMaintenanceOccurrenceId);
        if (entry.IsError)
        {
            return entry.Errors;
        }

        var copiedBlobs = new List<BlobRef>();
        foreach (var photoRef in cmd.PhotoRefs)
        {
            var copied = await CopyPhotoAsync(photoRef, ct);
            copiedBlobs.Add(copied.Reference);

            var added = entry.Value.AddPhoto(
                copied.Reference.Container,
                copied.Reference.Key,
                copied.Metadata.FileName ?? "photo",
                copied.Metadata.ContentType,
                copied.Metadata.Length);
            if (added.IsError)
            {
                await DeleteBlobsAsync(copiedBlobs, ct);
                return added.Errors;
            }
        }

        db.HistoryEntries.Add(entry.Value);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await DeleteBlobsAsync(copiedBlobs, ct);
            throw;
        }

        await audit.PublishAsync(cmd.HouseholdId, "property.history.created", "HistoryEntry", entry.Value.Id.Value, null, ct);
        return HistoryEntryResponse.FromEntry(entry.Value);
    }

    public async Task<ErrorOr<HistoryEntryResponse>> Handle(UpdateHistoryEntryCommand cmd, CancellationToken ct)
    {
        var type = ParseType(cmd.Type);
        if (type is null)
        {
            return PropertyErrors.HistoryEntryTypeInvalid;
        }

        var cost = ToMoney(cmd.Cost);
        if (cost.IsError)
        {
            return cost.Errors;
        }

        var entry = await LoadEntryAsync(cmd.HouseholdId, cmd.HistoryEntryId, ct);
        if (entry is null)
        {
            return PropertyErrors.HistoryEntryNotFound;
        }

        var updated = entry.Update(
            cmd.Date,
            cmd.Title,
            cmd.Area,
            cost.Value.Value,
            type.Value,
            cmd.SourceProjectId,
            cmd.SourceMaintenanceOccurrenceId);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.history.updated", "HistoryEntry", entry.Id.Value, null, ct);
        return HistoryEntryResponse.FromEntry(entry);
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteHistoryEntryCommand cmd, CancellationToken ct)
    {
        var entry = await LoadEntryAsync(cmd.HouseholdId, cmd.HistoryEntryId, ct);
        if (entry is null)
        {
            return PropertyErrors.HistoryEntryNotFound;
        }

        var blobs = entry.Photos
            .Select(photo => new BlobRef(photo.BlobContainer, photo.BlobKey))
            .ToArray();

        db.HistoryEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
        await DeleteBlobsAsync(blobs, ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.history.deleted", "HistoryEntry", cmd.HistoryEntryId, null, ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<ListHistoryResponse>> Handle(ListHistoryQuery query, CancellationToken ct)
    {
        var entries = db.HistoryEntries
            .AsNoTracking()
            .Include(entry => entry.Photos)
            .AsSplitQuery()
            .Where(entry => entry.HouseholdId == query.HouseholdId);

        if (query.Year is not null)
        {
            entries = entries.Where(entry => entry.Date.Year == query.Year);
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            entries = entries.Where(entry => entry.Area == query.Area);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = ParseType(query.Type);
            if (type is null)
            {
                return PropertyErrors.HistoryEntryTypeInvalid;
            }

            entries = entries.Where(entry => entry.Type == type.Value);
        }

        var items = await entries
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Id)
            .ToArrayAsync(ct);

        return new ListHistoryResponse(items.Select(HistoryEntryResponse.FromEntry).ToArray());
    }

    public async Task<ErrorOr<HistoryPhotoContentResponse>> Handle(GetHistoryPhotoQuery query, CancellationToken ct)
    {
        var entryId = new HistoryEntryId(query.HistoryEntryId);
        var photo = await db.HistoryEntries
            .AsNoTracking()
            .Where(entry => entry.HouseholdId == query.HouseholdId && entry.Id == entryId)
            .SelectMany(entry => entry.Photos)
            .SingleOrDefaultAsync(photo => photo.BlobKey == query.BlobKey, ct);
        if (photo is null)
        {
            return PropertyErrors.HistoryPhotoNotFound;
        }

        var content = await blobStore.GetAsync(new BlobRef(photo.BlobContainer, photo.BlobKey), ct);
        return new HistoryPhotoContentResponse(content.Stream, photo.ContentType, photo.FileName);
    }

    private async Task<CopiedPhoto> CopyPhotoAsync(HistoryPhotoRefRequest source, CancellationToken ct)
    {
        var content = await blobStore.GetAsync(new BlobRef(source.Container, source.Key), ct);
        await using (content.Stream.ConfigureAwait(false))
        {
            var copied = await blobStore.PutAsync(content.Stream, content.Metadata, ct);
            return new CopiedPhoto(copied, content.Metadata);
        }
    }

    private async Task<HistoryEntry?> LoadEntryAsync(Guid householdId, Guid historyEntryId, CancellationToken ct) =>
        await db.HistoryEntries
            .Include(entry => entry.Photos)
            .AsSplitQuery()
            .SingleOrDefaultAsync(entry => entry.HouseholdId == householdId && entry.Id == new HistoryEntryId(historyEntryId), ct);

    private async Task DeleteBlobsAsync(IEnumerable<BlobRef> blobs, CancellationToken ct)
    {
        foreach (var blob in blobs)
        {
            await blobStore.DeleteAsync(blob, ct);
        }
    }

    private static HistoryEntryType? ParseType(string type) =>
        Enum.TryParse<HistoryEntryType>(type, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private static ErrorOr<OptionalMoney> ToMoney(MoneyDto? money)
    {
        if (money is null)
        {
            return new OptionalMoney(null);
        }

        var created = Money.Create(money.Amount, money.Currency);
        return created.IsError ? created.Errors : new OptionalMoney(created.Value);
    }

    private sealed record CopiedPhoto(BlobRef Reference, BlobMetadata Metadata);
    private sealed record OptionalMoney(Money? Value);
}
