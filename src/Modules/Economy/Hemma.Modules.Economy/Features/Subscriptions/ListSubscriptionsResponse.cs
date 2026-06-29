using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record ListSubscriptionsResponse(IReadOnlyCollection<SubscriptionResponse> Subscriptions);
