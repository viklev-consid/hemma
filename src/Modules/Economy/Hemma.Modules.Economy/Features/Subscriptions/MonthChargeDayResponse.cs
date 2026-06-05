namespace Hemma.Modules.Economy.Features.Subscriptions;

public sealed record MonthChargeDayResponse(DateOnly Date, IReadOnlyList<MonthChargeResponse> Charges);
