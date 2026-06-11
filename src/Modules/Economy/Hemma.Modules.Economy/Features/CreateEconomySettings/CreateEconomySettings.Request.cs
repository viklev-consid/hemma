namespace Hemma.Modules.Economy.Features.CreateEconomySettings;

public sealed record CreateEconomySettingsRequest(Guid HouseholdId, int CycleStartDay, string DefaultCurrency);
