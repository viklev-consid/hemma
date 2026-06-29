using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record LinkCandidateResponse(Guid TransactionId, DateOnly OccurredOn, MoneyDto Amount, string? Note);
