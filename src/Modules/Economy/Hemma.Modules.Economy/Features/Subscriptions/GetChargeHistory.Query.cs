namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record GetChargeHistoryQuery(Guid HouseholdId, Guid SubscriptionId, int Page, int PageSize);
