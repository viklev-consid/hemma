using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record PriceChangeResponse(DateOnly ChangedOn, MoneyResponse PreviousAmount, MoneyResponse NewAmount);
