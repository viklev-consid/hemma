namespace Hemma.Modules.Property.Features.ReorderAreas;

public sealed record ReorderAreasCommand(Guid HouseholdId, IReadOnlyList<Guid> AreaIds);
