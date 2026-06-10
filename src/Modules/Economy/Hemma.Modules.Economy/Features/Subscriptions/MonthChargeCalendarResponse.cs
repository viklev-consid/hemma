using Hemma.Modules.Economy.Features.Contracts;

namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record MonthChargeCalendarResponse(
    DateOnly Month,
    IReadOnlyList<MonthChargeDayResponse> Days,
    MoneyResponse ActualTotal,
    MoneyResponse PredictedTotal);
