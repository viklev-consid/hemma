using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.AreasTags;

public sealed record PropertyAreaResponse(
    Guid AreaId,
    Guid HouseholdId,
    string Name,
    string? Description,
    int SortOrder,
    bool IsArchived)
{
    public static PropertyAreaResponse FromArea(PropertyArea area) =>
        new(area.Id.Value, area.HouseholdId, area.Name, area.Description, area.SortOrder, area.IsArchived);
}

public sealed record ListAreasResponse(IReadOnlyList<PropertyAreaResponse> Areas);

public sealed record PropertyTagResponse(Guid TagId, Guid HouseholdId, string Name, string? Color, bool IsArchived)
{
    public static PropertyTagResponse FromTag(PropertyTag tag) =>
        new(tag.Id.Value, tag.HouseholdId, tag.Name, tag.Color, tag.IsArchived);
}

public sealed record ListTagsResponse(IReadOnlyList<PropertyTagResponse> Tags);

public sealed record AssignTagsResponse(string TargetType, Guid TargetId, IReadOnlyList<PropertyTagResponse> Tags);
