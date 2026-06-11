namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record UnlinkTransactionCommand(Guid HouseholdId, Guid SubscriptionId, Guid TransactionId);
