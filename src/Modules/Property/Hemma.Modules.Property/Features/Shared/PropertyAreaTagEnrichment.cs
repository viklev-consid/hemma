using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.Shared;

/// <summary>
/// Resolves canonical area names and assigned tags for read/response enrichment.
/// Area names are populated on every area-bearing response so callers never have to
/// resolve <c>areaId -&gt; name</c> client-side; tags are populated on read surfaces.
/// </summary>
public static class PropertyAreaTagEnrichment
{
    public static async Task<string?> AreaNameAsync(PropertyDbContext db, Guid householdId, Guid? areaId, CancellationToken ct)
    {
        if (areaId is null)
        {
            return null;
        }

        var typedId = new PropertyAreaId(areaId.Value);
        return await db.Areas
            .AsNoTracking()
            .Where(area => area.HouseholdId == householdId && area.Id == typedId)
            .Select(area => area.Name)
            .FirstOrDefaultAsync(ct);
    }

    public static async Task<IReadOnlyDictionary<Guid, string>> AreaNameMapAsync(PropertyDbContext db, Guid householdId, CancellationToken ct) =>
        await db.Areas
            .AsNoTracking()
            .Where(area => area.HouseholdId == householdId)
            .ToDictionaryAsync(area => area.Id.Value, area => area.Name, ct);

    public static async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PropertyTagResponse>>> TagsByTargetAsync(
        PropertyDbContext db,
        Guid householdId,
        PropertyTagTargetType targetType,
        IReadOnlyCollection<Guid> targetIds,
        CancellationToken ct)
    {
        if (targetIds.Count == 0)
        {
            return EmptyTags;
        }

        var rows = await db.TagAssignments
            .AsNoTracking()
            .Where(assignment => assignment.HouseholdId == householdId
                && assignment.TargetType == targetType
                && targetIds.Contains(assignment.TargetId))
            .Join(
                db.Tags.AsNoTracking(),
                assignment => assignment.TagId,
                tag => tag.Id,
                (assignment, tag) => new { assignment.TargetId, Tag = tag })
            .ToArrayAsync(ct);

        return rows
            .GroupBy(row => row.TargetId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PropertyTagResponse>)group
                    .Select(row => row.Tag)
                    .OrderBy(tag => tag.Name, StringComparer.Ordinal)
                    .Select(PropertyTagResponse.FromTag)
                    .ToArray());
    }

    public static async Task<IReadOnlyList<PropertyTagResponse>> TagsForTargetAsync(
        PropertyDbContext db,
        Guid householdId,
        PropertyTagTargetType targetType,
        Guid targetId,
        CancellationToken ct)
    {
        var byTarget = await TagsByTargetAsync(db, householdId, targetType, [targetId], ct);
        return byTarget.GetValueOrDefault(targetId, []);
    }

    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<PropertyTagResponse>> EmptyTags =
        new Dictionary<Guid, IReadOnlyList<PropertyTagResponse>>();
}
