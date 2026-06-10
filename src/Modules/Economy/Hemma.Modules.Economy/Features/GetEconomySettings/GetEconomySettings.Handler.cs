using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.GetEconomySettings;

public sealed class GetEconomySettingsHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<GetEconomySettingsResponse>> Handle(GetEconomySettingsQuery query, CancellationToken ct)
    {
        var settings = await db.EconomySettings
            .AsNoTracking()
            .SingleOrDefaultAsync(settings => settings.HouseholdId == query.HouseholdId, ct);

        return settings is null
            ? EconomyErrors.SettingsNotFound
            : GetEconomySettingsResponse.From(settings);
    }
}
