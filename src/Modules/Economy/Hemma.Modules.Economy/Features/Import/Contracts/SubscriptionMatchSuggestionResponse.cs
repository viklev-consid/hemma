using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Import.Contracts;

public sealed record SubscriptionMatchSuggestionResponse(
    Guid SubscriptionId,
    string Name,
    string MatchState,
    MoneyResponse ExpectedAmount);
