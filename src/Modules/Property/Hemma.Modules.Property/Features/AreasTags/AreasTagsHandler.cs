using ErrorOr;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.AreasTags;

public sealed class AreasTagsHandler(PropertyDbContext db, PropertyAuditPublisher audit)
{
    public async Task<ErrorOr<PropertyAreaResponse>> Handle(CreateAreaCommand cmd, CancellationToken ct)
    {
        if (await AreaNameExistsAsync(cmd.HouseholdId, cmd.Name, null, ct))
        {
            return PropertyErrors.AreaNameAlreadyExists;
        }

        var sortOrder = await db.Areas.CountAsync(area => area.HouseholdId == cmd.HouseholdId, ct);
        var area = PropertyArea.Create(cmd.HouseholdId, cmd.Name, cmd.Description, sortOrder);
        if (area.IsError)
        {
            return area.Errors;
        }

        db.Areas.Add(area.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.area.created", "PropertyArea", area.Value.Id.Value, null, ct);
        return PropertyAreaResponse.FromArea(area.Value);
    }

    public async Task<ErrorOr<PropertyAreaResponse>> Handle(UpdateAreaCommand cmd, CancellationToken ct)
    {
        var area = await db.Areas.SingleOrDefaultAsync(area => area.HouseholdId == cmd.HouseholdId && area.Id == new PropertyAreaId(cmd.AreaId), ct);
        if (area is null)
        {
            return PropertyErrors.AreaNotFound;
        }

        if (await AreaNameExistsAsync(cmd.HouseholdId, cmd.Name, area.Id, ct))
        {
            return PropertyErrors.AreaNameAlreadyExists;
        }

        var updated = area.Update(cmd.Name, cmd.Description);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.area.updated", "PropertyArea", area.Id.Value, null, ct);
        return PropertyAreaResponse.FromArea(area);
    }

    public async Task<ErrorOr<PropertyAreaResponse>> Handle(ArchiveAreaCommand cmd, CancellationToken ct)
    {
        var area = await db.Areas.SingleOrDefaultAsync(area => area.HouseholdId == cmd.HouseholdId && area.Id == new PropertyAreaId(cmd.AreaId), ct);
        if (area is null)
        {
            return PropertyErrors.AreaNotFound;
        }

        area.Archive();
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.area.archived", "PropertyArea", area.Id.Value, null, ct);
        return PropertyAreaResponse.FromArea(area);
    }

    public async Task<ErrorOr<ListAreasResponse>> Handle(ReorderAreasCommand cmd, CancellationToken ct)
    {
        var areas = await db.Areas
            .Where(area => area.HouseholdId == cmd.HouseholdId && !area.IsArchived)
            .ToListAsync(ct);

        if (cmd.AreaIds.Count != areas.Count || cmd.AreaIds.Distinct().Count() != areas.Count)
        {
            return PropertyErrors.AreaOrderInvalid;
        }

        var byId = areas.ToDictionary(area => area.Id.Value);
        if (cmd.AreaIds.Any(id => !byId.ContainsKey(id)))
        {
            return PropertyErrors.AreaOrderInvalid;
        }

        var sortOrder = 0;
        foreach (var areaId in cmd.AreaIds)
        {
            byId[areaId].SetSortOrder(sortOrder++);
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.area.reordered", "PropertyArea", cmd.HouseholdId, null, ct);
        return new ListAreasResponse(areas.OrderBy(area => area.SortOrder).Select(PropertyAreaResponse.FromArea).ToArray());
    }

    public async Task<ErrorOr<ListAreasResponse>> Handle(ListAreasQuery query, CancellationToken ct)
    {
        var areas = db.Areas.AsNoTracking().Where(area => area.HouseholdId == query.HouseholdId);
        if (!query.IncludeArchived)
        {
            areas = areas.Where(area => !area.IsArchived);
        }

        var items = await areas
            .OrderBy(area => area.SortOrder)
            .ThenBy(area => area.Name)
            .Select(area => PropertyAreaResponse.FromArea(area))
            .ToArrayAsync(ct);

        return new ListAreasResponse(items);
    }

    public async Task<ErrorOr<PropertyTagResponse>> Handle(CreateTagCommand cmd, CancellationToken ct)
    {
        if (await TagNameExistsAsync(cmd.HouseholdId, cmd.Name, null, ct))
        {
            return PropertyErrors.TagNameAlreadyExists;
        }

        var tag = PropertyTag.Create(cmd.HouseholdId, cmd.Name, cmd.Color);
        if (tag.IsError)
        {
            return tag.Errors;
        }

        db.Tags.Add(tag.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.tag.created", "PropertyTag", tag.Value.Id.Value, null, ct);
        return PropertyTagResponse.FromTag(tag.Value);
    }

    public async Task<ErrorOr<PropertyTagResponse>> Handle(UpdateTagCommand cmd, CancellationToken ct)
    {
        var tag = await db.Tags.SingleOrDefaultAsync(tag => tag.HouseholdId == cmd.HouseholdId && tag.Id == new PropertyTagId(cmd.TagId), ct);
        if (tag is null)
        {
            return PropertyErrors.TagNotFound;
        }

        if (await TagNameExistsAsync(cmd.HouseholdId, cmd.Name, tag.Id, ct))
        {
            return PropertyErrors.TagNameAlreadyExists;
        }

        var updated = tag.Update(cmd.Name, cmd.Color);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.tag.updated", "PropertyTag", tag.Id.Value, null, ct);
        return PropertyTagResponse.FromTag(tag);
    }

    public async Task<ErrorOr<PropertyTagResponse>> Handle(ArchiveTagCommand cmd, CancellationToken ct)
    {
        var tag = await db.Tags.SingleOrDefaultAsync(tag => tag.HouseholdId == cmd.HouseholdId && tag.Id == new PropertyTagId(cmd.TagId), ct);
        if (tag is null)
        {
            return PropertyErrors.TagNotFound;
        }

        tag.Archive();
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.tag.archived", "PropertyTag", tag.Id.Value, null, ct);
        return PropertyTagResponse.FromTag(tag);
    }

    public async Task<ErrorOr<ListTagsResponse>> Handle(ListTagsQuery query, CancellationToken ct)
    {
        var tags = db.Tags.AsNoTracking().Where(tag => tag.HouseholdId == query.HouseholdId);
        if (!query.IncludeArchived)
        {
            tags = tags.Where(tag => !tag.IsArchived);
        }

        var items = await tags
            .OrderBy(tag => tag.Name)
            .Select(tag => PropertyTagResponse.FromTag(tag))
            .ToArrayAsync(ct);

        return new ListTagsResponse(items);
    }

    public async Task<ErrorOr<AssignTagsResponse>> Handle(AssignTagsCommand cmd, CancellationToken ct)
    {
        var targetType = ParseTargetType(cmd.TargetType);
        if (targetType is null)
        {
            return PropertyErrors.TagTargetTypeInvalid;
        }

        if (!await TargetExistsAsync(cmd.HouseholdId, targetType.Value, cmd.TargetId, ct))
        {
            return PropertyErrors.TagTargetNotFound;
        }

        var uniqueTagIds = cmd.TagIds.Distinct().Select(id => new PropertyTagId(id)).ToArray();
        var tags = await db.Tags
            .Where(tag => tag.HouseholdId == cmd.HouseholdId && uniqueTagIds.Contains(tag.Id))
            .ToListAsync(ct);

        if (tags.Count != uniqueTagIds.Length)
        {
            return PropertyErrors.TagAssignmentInvalid;
        }

        await db.TagAssignments
            .Where(assignment => assignment.HouseholdId == cmd.HouseholdId
                && assignment.TargetType == targetType.Value
                && assignment.TargetId == cmd.TargetId)
            .ExecuteDeleteAsync(ct);

        foreach (var tag in tags)
        {
            var assignment = PropertyTagAssignment.Create(cmd.HouseholdId, tag.Id, targetType.Value, cmd.TargetId);
            if (assignment.IsError)
            {
                return assignment.Errors;
            }

            db.TagAssignments.Add(assignment.Value);
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.tags.assigned", targetType.Value.ToString(), cmd.TargetId, null, ct);
        return new AssignTagsResponse(
            targetType.Value.ToString(),
            cmd.TargetId,
            tags.OrderBy(tag => tag.Name, StringComparer.Ordinal).Select(PropertyTagResponse.FromTag).ToArray());
    }

    private async Task<bool> AreaNameExistsAsync(Guid householdId, string name, PropertyAreaId? exceptId, CancellationToken ct)
    {
        return await db.Areas.AnyAsync(area =>
            area.HouseholdId == householdId &&
            EF.Functions.ILike(area.Name, name.Trim()) &&
            (exceptId == null || area.Id != exceptId), ct);
    }

    private async Task<bool> TagNameExistsAsync(Guid householdId, string name, PropertyTagId? exceptId, CancellationToken ct)
    {
        return await db.Tags.AnyAsync(tag =>
            tag.HouseholdId == householdId &&
            EF.Functions.ILike(tag.Name, name.Trim()) &&
            (exceptId == null || tag.Id != exceptId), ct);
    }

    private static PropertyTagTargetType? ParseTargetType(string targetType) =>
        Enum.TryParse<PropertyTagTargetType>(targetType, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private async Task<bool> TargetExistsAsync(Guid householdId, PropertyTagTargetType targetType, Guid targetId, CancellationToken ct) =>
        targetType switch
        {
            PropertyTagTargetType.Project => await db.Projects.AnyAsync(project => project.HouseholdId == householdId && project.Id == new ProjectId(targetId), ct),
            PropertyTagTargetType.MaintenancePlan => await db.MaintenancePlans.AnyAsync(plan => plan.HouseholdId == householdId && plan.Id == new MaintenancePlanId(targetId), ct),
            PropertyTagTargetType.MaintenanceOccurrence => await db.MaintenanceOccurrences.AnyAsync(occurrence => occurrence.HouseholdId == householdId && occurrence.Id == new MaintenanceOccurrenceId(targetId), ct),
            PropertyTagTargetType.HistoryEntry => await db.HistoryEntries.AnyAsync(entry => entry.HouseholdId == householdId && entry.Id == new HistoryEntryId(targetId), ct),
            PropertyTagTargetType.Issue => await db.Issues.AnyAsync(issue => issue.HouseholdId == householdId && issue.Id == new PropertyIssueId(targetId), ct),
            _ => false
        };
}
