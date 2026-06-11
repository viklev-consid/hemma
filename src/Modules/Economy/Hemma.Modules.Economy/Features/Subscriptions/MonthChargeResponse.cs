using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record MonthChargeResponse(Guid SubscriptionId, string Name, MoneyDto Amount, string MatchState, Guid? TransactionId);
