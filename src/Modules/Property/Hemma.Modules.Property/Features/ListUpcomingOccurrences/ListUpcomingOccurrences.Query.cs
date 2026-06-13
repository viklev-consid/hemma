namespace Hemma.Modules.Property.Features.ListUpcomingOccurrences;

public sealed record ListUpcomingOccurrencesQuery(Guid HouseholdId, int HorizonDays, bool? IsOverdue);
