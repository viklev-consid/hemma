using ErrorOr;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Features.AddAttachment;
using Hemma.Modules.Property.Features.AddLink;
using Hemma.Modules.Property.Features.AddTask;
using Hemma.Modules.Property.Features.ArchiveArea;
using Hemma.Modules.Property.Features.ArchiveTag;
using Hemma.Modules.Property.Features.AssignTags;
using Hemma.Modules.Property.Features.ChangeIssueStatus;
using Hemma.Modules.Property.Features.ChangeProjectStatus;
using Hemma.Modules.Property.Features.CompleteOccurrence;
using Hemma.Modules.Property.Features.CreateArea;
using Hemma.Modules.Property.Features.CreateHistoryEntry;
using Hemma.Modules.Property.Features.CreateMaintenancePlan;
using Hemma.Modules.Property.Features.CreateProject;
using Hemma.Modules.Property.Features.CreateTag;
using Hemma.Modules.Property.Features.DeactivatePlan;
using Hemma.Modules.Property.Features.DeleteHistoryEntry;
using Hemma.Modules.Property.Features.DeleteIssue;
using Hemma.Modules.Property.Features.DeletePlan;
using Hemma.Modules.Property.Features.DeleteProject;
using Hemma.Modules.Property.Features.DeleteTask;
using Hemma.Modules.Property.Features.GetAttachmentContent;
using Hemma.Modules.Property.Features.GetHistoryPhoto;
using Hemma.Modules.Property.Features.GetIssue;
using Hemma.Modules.Property.Features.GetMaintenancePlan;
using Hemma.Modules.Property.Features.GetProject;
using Hemma.Modules.Property.Features.GetProjectBudget;
using Hemma.Modules.Property.Features.GetProjectTasks;
using Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;
using Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;
using Hemma.Modules.Property.Features.ListAreas;
using Hemma.Modules.Property.Features.ListHistory;
using Hemma.Modules.Property.Features.ListIssues;
using Hemma.Modules.Property.Features.ListMaintenancePlans;
using Hemma.Modules.Property.Features.ListProjects;
using Hemma.Modules.Property.Features.ListTags;
using Hemma.Modules.Property.Features.ListUpcomingOccurrences;
using Hemma.Modules.Property.Features.PromoteIssueToProject;
using Hemma.Modules.Property.Features.PromoteOccurrenceToProject;
using Hemma.Modules.Property.Features.RemoveAttachment;
using Hemma.Modules.Property.Features.RemoveLink;
using Hemma.Modules.Property.Features.ReorderAreas;
using Hemma.Modules.Property.Features.ReorderTasks;
using Hemma.Modules.Property.Features.ReportIssue;
using Hemma.Modules.Property.Features.SkipOccurrence;
using Hemma.Modules.Property.Features.UnlinkIssue;
using Hemma.Modules.Property.Features.UpdateArea;
using Hemma.Modules.Property.Features.UpdateHistoryEntry;
using Hemma.Modules.Property.Features.UpdateIssue;
using Hemma.Modules.Property.Features.UpdateMaintenancePlan;
using Hemma.Modules.Property.Features.UpdateProject;
using Hemma.Modules.Property.Features.UpdateTag;
using Hemma.Modules.Property.Features.UpdateTask;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Contracts;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.Shared;

public sealed class LogbookOperations(
    PropertyDbContext db,
    IBlobStore blobStore,
    PropertyAuditPublisher audit,
    ActivityOperations activity)
{
    public async Task<ErrorOr<HistoryEntryResponse>> CreateHistoryEntryAsync(CreateHistoryEntryCommand cmd, CancellationToken ct)
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

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var entry = HistoryEntry.Create(
            cmd.HouseholdId,
            cmd.Date,
            cmd.Title,
            areaId.Value.Value,
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
            var copied = await CopyPhotoAsync(cmd.HouseholdId, photoRef, ct);
            if (copied.IsError)
            {
                await DeleteBlobsAsync(copiedBlobs, ct);
                return copied.Errors;
            }

            copiedBlobs.Add(copied.Value.Reference);

            var added = entry.Value.AddPhoto(
                copied.Value.Reference.Container,
                copied.Value.Reference.Key,
                copied.Value.Metadata.FileName ?? "photo",
                copied.Value.Metadata.ContentType,
                copied.Value.Metadata.Length);
            if (added.IsError)
            {
                await DeleteBlobsAsync(copiedBlobs, ct);
                return added.Errors;
            }
        }

        db.HistoryEntries.Add(entry.Value);
        var activityResult = activity.Append(
            cmd.HouseholdId,
            PropertyActivityVerb.HistoryEntryCreated,
            PropertyActivityTargetType.HistoryEntry,
            entry.Value.Id.Value,
            $"History entry \"{entry.Value.Title}\" was added.",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["type"] = entry.Value.Type.ToString()
            });
        if (activityResult.IsError)
        {
            await DeleteBlobsAsync(copiedBlobs, ct);
            return activityResult.Errors;
        }

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
        return await EnrichEntryAsync(HistoryEntryResponse.FromEntry(entry.Value), includeTags: false, ct);
    }

    public async Task<ErrorOr<HistoryEntryResponse>> UpdateHistoryEntryAsync(UpdateHistoryEntryCommand cmd, CancellationToken ct)
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

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var updated = entry.Update(
            cmd.Date,
            cmd.Title,
            areaId.Value.Value,
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
        return await EnrichEntryAsync(HistoryEntryResponse.FromEntry(entry), includeTags: false, ct);
    }

    public async Task<ErrorOr<Deleted>> DeleteHistoryEntryAsync(DeleteHistoryEntryCommand cmd, CancellationToken ct)
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

    public async Task<ErrorOr<ListHistoryResponse>> ListHistoryAsync(ListHistoryQuery query, CancellationToken ct)
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

        if (query.AreaId is not null)
        {
            entries = entries.Where(entry => entry.AreaId == new PropertyAreaId(query.AreaId.Value));
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

        if (query.TagIds is { Count: > 0 })
        {
            var tagIds = query.TagIds.Distinct().Select(id => new PropertyTagId(id)).ToArray();
            var matchingEntryIds = (await db.TagAssignments
                .AsNoTracking()
                .Where(assignment => assignment.HouseholdId == query.HouseholdId
                    && assignment.TargetType == PropertyTagTargetType.HistoryEntry
                    && tagIds.Contains(assignment.TagId))
                .GroupBy(assignment => assignment.TargetId)
                .Where(group => group.Select(assignment => assignment.TagId).Distinct().Count() == tagIds.Length)
                .Select(group => group.Key)
                .ToArrayAsync(ct))
                .Select(id => new HistoryEntryId(id))
                .ToArray();

            entries = entries.Where(entry => matchingEntryIds.Contains(entry.Id));
        }

        var items = await entries
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Id)
            .ToArrayAsync(ct);

        var areaNames = await PropertyAreaTagEnrichment.AreaNameMapAsync(db, query.HouseholdId, ct);
        var tagsByEntry = await PropertyAreaTagEnrichment.TagsByTargetAsync(
            db, query.HouseholdId, PropertyTagTargetType.HistoryEntry, items.Select(entry => entry.Id.Value).ToArray(), ct);

        var responses = items
            .Select(entry =>
            {
                var response = HistoryEntryResponse.FromEntry(entry);
                return response with
                {
                    AreaName = response.AreaId is null ? null : areaNames.GetValueOrDefault(response.AreaId.Value),
                    Tags = tagsByEntry.GetValueOrDefault(entry.Id.Value, [])
                };
            })
            .ToArray();

        return new ListHistoryResponse(responses);
    }

    public async Task<ErrorOr<HistoryPhotoContentResponse>> GetHistoryPhotoAsync(GetHistoryPhotoQuery query, CancellationToken ct)
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

    private async Task<HistoryEntryResponse> EnrichEntryAsync(HistoryEntryResponse response, bool includeTags, CancellationToken ct)
    {
        var areaName = await PropertyAreaTagEnrichment.AreaNameAsync(db, response.HouseholdId, response.AreaId, ct);
        var tags = includeTags
            ? await PropertyAreaTagEnrichment.TagsForTargetAsync(db, response.HouseholdId, PropertyTagTargetType.HistoryEntry, response.HistoryEntryId, ct)
            : response.Tags;
        return response with { AreaName = areaName, Tags = tags };
    }

    private async Task<ErrorOr<CopiedPhoto>> CopyPhotoAsync(Guid householdId, HistoryPhotoRefRequest source, CancellationToken ct)
    {
        var authorizedSource = await ResolveOwnedPhotoSourceAsync(householdId, source, ct);
        if (authorizedSource is null)
        {
            return PropertyErrors.AttachmentNotFound;
        }

        var content = await blobStore.GetAsync(new BlobRef(authorizedSource.Container, authorizedSource.Key), ct);
        await using (content.Stream.ConfigureAwait(false))
        {
            var copied = await blobStore.PutAsync(content.Stream, content.Metadata, ct);
            return new CopiedPhoto(copied, content.Metadata);
        }
    }

    private async Task<PhotoSource?> ResolveOwnedPhotoSourceAsync(Guid householdId, HistoryPhotoRefRequest source, CancellationToken ct)
    {
        var normalizedContainer = source.Container.Trim();
        var normalizedKey = source.Key.Trim();

        var projectAttachment = await db.Projects
            .AsNoTracking()
            .Where(project => project.HouseholdId == householdId)
            .SelectMany(project => project.Attachments)
            .Where(attachment => attachment.BlobContainer == normalizedContainer && attachment.BlobKey == normalizedKey)
            .Select(attachment => new PhotoSource(attachment.BlobContainer, attachment.BlobKey))
            .FirstOrDefaultAsync(ct);
        if (projectAttachment is not null)
        {
            return projectAttachment;
        }

        return await db.HistoryEntries
            .AsNoTracking()
            .Where(entry => entry.HouseholdId == householdId)
            .SelectMany(entry => entry.Photos)
            .Where(photo => photo.BlobContainer == normalizedContainer && photo.BlobKey == normalizedKey)
            .Select(photo => new PhotoSource(photo.BlobContainer, photo.BlobKey))
            .FirstOrDefaultAsync(ct);
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
    private sealed record PhotoSource(string Container, string Key);
    private sealed record OptionalMoney(Money? Value);

    private async Task<ErrorOr<OptionalAreaId>> ValidateAreaAsync(Guid householdId, Guid? areaId, CancellationToken ct)
    {
        if (areaId is null)
        {
            return new OptionalAreaId(null);
        }

        var typedId = new PropertyAreaId(areaId.Value);
        var exists = await db.Areas.AnyAsync(area => area.HouseholdId == householdId && area.Id == typedId, ct);
        return exists ? new OptionalAreaId(typedId) : PropertyErrors.AreaNotFound;
    }

    private sealed record OptionalAreaId(PropertyAreaId? Value);
}
