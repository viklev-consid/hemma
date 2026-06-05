namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record LinkTransactionCommand(Guid HouseholdId, Guid SubscriptionId, Guid TransactionId);
