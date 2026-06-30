using Hemma.Shared.Contracts;

namespace Hemma.Modules.Economy.Features.Import.Contracts;

public sealed record RecurringBillMatchSuggestionResponse(
    Guid RecurringBillId,
    Guid OccurrenceId,
    string Name,
    DateOnly DueOn,
    MoneyDto ExpectedAmount,
    int DayDelta,
    decimal AmountDelta);
