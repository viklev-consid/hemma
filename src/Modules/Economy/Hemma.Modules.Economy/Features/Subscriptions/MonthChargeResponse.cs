using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record MonthChargeResponse(Guid SubscriptionId, string Name, MoneyResponse Amount, string MatchState, Guid? TransactionId);
