using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.Features.GetEconomySettings;

public sealed record GetEconomySettingsResponse(
    Guid SettingsId,
    Guid HouseholdId,
    int CycleStartDay,
    string DefaultCurrency,
    DateOnly CreatedOn)
{
    public static GetEconomySettingsResponse From(EconomySettings settings) =>
        new(
            settings.Id.Value,
            settings.HouseholdId,
            settings.CycleStartDay,
            settings.DefaultCurrency,
            settings.CreatedOn);
}
