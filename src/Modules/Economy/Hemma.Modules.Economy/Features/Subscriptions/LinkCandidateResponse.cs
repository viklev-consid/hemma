using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record LinkCandidateResponse(Guid TransactionId, DateOnly OccurredOn, MoneyResponse Amount, string? Note);
