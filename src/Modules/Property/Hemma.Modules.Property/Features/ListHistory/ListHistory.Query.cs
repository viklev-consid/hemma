namespace Hemma.Modules.Property.Features.ListHistory;

public sealed record ListHistoryQuery(Guid HouseholdId, int? Year, Guid? AreaId, string? Type, IReadOnlyList<Guid>? TagIds);
