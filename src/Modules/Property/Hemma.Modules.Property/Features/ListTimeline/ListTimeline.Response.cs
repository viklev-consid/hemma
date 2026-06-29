using Hemma.Shared.Contracts;

namespace Hemma.Modules.Property.Features.ListTimeline;

public sealed record TimelineItemResponse(
    string SourceType,
    Guid SourceId,
    DateOnly Date,
    string Title,
    Guid? AreaId,
    string? AreaName,
    MoneyDto? Cost,
    string Type,
    IReadOnlyList<TimelineTagResponse> Tags,
    int PhotoCount);

public sealed record TimelineTagResponse(Guid TagId, string Name, string? Color);

public sealed record ListTimelineResponse(IReadOnlyList<TimelineItemResponse> Items, bool HasMore = false, int? TotalCount = null);
