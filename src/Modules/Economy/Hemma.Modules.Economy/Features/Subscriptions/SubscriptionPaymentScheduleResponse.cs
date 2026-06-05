namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record SubscriptionPaymentScheduleResponse(Guid SubscriptionId, string Name, IReadOnlyList<int> Months);
