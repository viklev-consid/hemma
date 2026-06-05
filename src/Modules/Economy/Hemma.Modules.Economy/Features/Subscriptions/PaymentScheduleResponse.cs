namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record PaymentScheduleResponse(int Year, IReadOnlyList<SubscriptionPaymentScheduleResponse> Subscriptions);
