namespace Hemma.Modules.Property.Features.AreasTags;

public sealed record PropertyAreaRequest(Guid HouseholdId, string Name, string? Description);

public sealed record ReorderAreasRequest(Guid HouseholdId, IReadOnlyList<Guid> AreaIds);

public sealed record PropertyTagRequest(Guid HouseholdId, string Name, string? Color);

public sealed record AssignTagsRequest(Guid HouseholdId, string TargetType, Guid TargetId, IReadOnlyList<Guid> TagIds);
