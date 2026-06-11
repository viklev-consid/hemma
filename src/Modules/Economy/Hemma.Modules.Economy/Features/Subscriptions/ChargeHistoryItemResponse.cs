using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record ChargeHistoryItemResponse(Guid TransactionId, DateOnly OccurredOn, MoneyResponse Amount, string? Note, string MatchState);
