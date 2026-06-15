namespace Hemma.Modules.Property.Features.ListTimeline;

public sealed record ListTimelineQuery(Guid HouseholdId, int? Year, Guid? AreaId, string? Type, IReadOnlyList<Guid>? TagIds, int? Offset, int? Limit);
