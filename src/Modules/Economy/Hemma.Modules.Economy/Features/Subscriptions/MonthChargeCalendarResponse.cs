namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record MonthChargeCalendarResponse(DateOnly Month, IReadOnlyList<MonthChargeDayResponse> Days);
