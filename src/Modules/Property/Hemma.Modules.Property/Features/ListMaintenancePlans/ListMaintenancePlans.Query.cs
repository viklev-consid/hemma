namespace Hemma.Modules.Property.Features.ListMaintenancePlans;

public sealed record ListMaintenancePlansQuery(Guid HouseholdId, bool? ActiveOnly, Guid? AreaId, IReadOnlyList<Guid>? TagIds);
