namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record ChargeHistoryResponse(Guid SubscriptionId, IReadOnlyList<ChargeHistoryItemResponse> Charges, IReadOnlyList<PriceChangeResponse> PriceChanges);
