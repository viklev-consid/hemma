using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record ChargeHistoryItemResponse(Guid TransactionId, DateOnly OccurredOn, MoneyDto Amount, string? Note, string MatchState);
