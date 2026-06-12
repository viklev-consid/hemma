namespace Hemma.Modules.Property.Features.AreasTags;

public sealed record CreateAreaCommand(Guid HouseholdId, string Name, string? Description);

public sealed record UpdateAreaCommand(Guid AreaId, Guid HouseholdId, string Name, string? Description);

public sealed record ArchiveAreaCommand(Guid AreaId, Guid HouseholdId);

public sealed record ReorderAreasCommand(Guid HouseholdId, IReadOnlyList<Guid> AreaIds);

public sealed record ListAreasQuery(Guid HouseholdId, bool IncludeArchived);

public sealed record CreateTagCommand(Guid HouseholdId, string Name, string? Color);

public sealed record UpdateTagCommand(Guid TagId, Guid HouseholdId, string Name, string? Color);

public sealed record ArchiveTagCommand(Guid TagId, Guid HouseholdId);

public sealed record ListTagsQuery(Guid HouseholdId, bool IncludeArchived);

public sealed record AssignTagsCommand(Guid HouseholdId, string TargetType, Guid TargetId, IReadOnlyList<Guid> TagIds);
