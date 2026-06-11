using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record ListSubscriptionsResponse(IReadOnlyCollection<SubscriptionResponse> Subscriptions);
