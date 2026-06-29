using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record PriceChangeResponse(DateOnly ChangedOn, MoneyDto PreviousAmount, MoneyDto NewAmount);
