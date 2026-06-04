namespace Hemma.Modules.Economy.Features.CreateEconomySettings;

public sealed record CreateEconomySettingsCommand(Guid HouseholdId, int CycleStartDay, string DefaultCurrency);
