using ErrorOr;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.ListTimeline;

public sealed class ListTimelineHandler(PropertyDbContext db)
{
    public async Task<ErrorOr<ListTimelineResponse>> Handle(ListTimelineQuery query, CancellationToken ct)
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

        var rows = await entries
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new
            {
                entry.Id,
                entry.Date,
                entry.Title,
                AreaId = entry.AreaId == null ? (Guid?)null : entry.AreaId.Value.Value,
                Cost = entry.Cost == null ? null : new MoneyDto(entry.Cost.Amount, entry.Cost.Currency),
                entry.Type,
                PhotoCount = entry.Photos.Count
            })
            .ToArrayAsync(ct);

        var sourceIds = rows.Select(row => row.Id.Value).ToArray();
        var tags = await db.TagAssignments
            .AsNoTracking()
            .Where(assignment => assignment.HouseholdId == query.HouseholdId
                && assignment.TargetType == PropertyTagTargetType.HistoryEntry
                && sourceIds.Contains(assignment.TargetId))
            .Join(
                db.Tags.AsNoTracking(),
                assignment => assignment.TagId,
                tag => tag.Id,
                (assignment, tag) => new
                {
                    assignment.TargetId,
                    Tag = new TimelineTagResponse(tag.Id.Value, tag.Name, tag.Color)
                })
            .ToArrayAsync(ct);

        var tagsByTarget = tags
            .GroupBy(row => row.TargetId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<TimelineTagResponse>)group.Select(row => row.Tag).ToArray());

        var items = rows
            .Select(row => new TimelineItemResponse(
                "HistoryEntry",
                row.Id.Value,
                row.Date,
                row.Title,
                row.AreaId,
                null,
                row.Cost,
                row.Type.ToString(),
                tagsByTarget.GetValueOrDefault(row.Id.Value, []),
                row.PhotoCount))
            .ToArray();

        return new ListTimelineResponse(items);
    }

    private static HistoryEntryType? ParseType(string type) =>
        Enum.TryParse<HistoryEntryType>(type, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
}
