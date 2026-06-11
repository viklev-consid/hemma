namespace Hemma.Modules.Economy.Features.UpdateCycleStartDay;

public sealed record UpdateCycleStartDayCommand(Guid HouseholdId, int CycleStartDay);
