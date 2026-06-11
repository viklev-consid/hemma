namespace Hemma.Modules.Economy.Features.UpdateCycleStartDay;

public sealed record UpdateCycleStartDayResponse(Guid SettingsId, Guid HouseholdId, int CycleStartDay);
