namespace Hemma.Modules.Property.Features.ReorderAreas;

public sealed record ReorderAreasRequest(Guid HouseholdId, IReadOnlyList<Guid> AreaIds);
