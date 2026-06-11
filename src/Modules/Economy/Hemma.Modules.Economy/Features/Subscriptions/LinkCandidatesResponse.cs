namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record LinkCandidatesResponse(Guid SubscriptionId, IReadOnlyCollection<LinkCandidateResponse> Candidates);
