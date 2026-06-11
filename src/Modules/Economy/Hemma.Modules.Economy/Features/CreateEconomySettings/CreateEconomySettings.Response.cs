namespace Hemma.Modules.Economy.Features.CreateEconomySettings;

public sealed record CreateEconomySettingsResponse(
    Guid SettingsId,
    Guid HouseholdId,
    int CycleStartDay,
    string DefaultCurrency,
    DateOnly CreatedOn);
