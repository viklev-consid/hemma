namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record GetMonthChargeCalendarQuery(Guid HouseholdId, DateOnly Month);
