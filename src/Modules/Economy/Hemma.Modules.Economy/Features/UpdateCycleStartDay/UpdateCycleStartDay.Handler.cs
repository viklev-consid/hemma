using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.UpdateCycleStartDay;

public sealed class UpdateCycleStartDayHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<UpdateCycleStartDayResponse>> Handle(UpdateCycleStartDayCommand cmd, CancellationToken ct)
    {
        var settings = await db.EconomySettings
            .SingleOrDefaultAsync(value => value.HouseholdId == cmd.HouseholdId, ct);
        if (settings is null)
        {
            return EconomyErrors.SettingsNotFound;
        }

        var update = settings.UpdateCycleStartDay(cmd.CycleStartDay);
        if (update.IsError)
        {
            return update.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(settings.HouseholdId, "economy.settings.cycle_start_updated", "EconomySettings", settings.Id.Value, null, ct);

        return new UpdateCycleStartDayResponse(settings.Id.Value, settings.HouseholdId, settings.CycleStartDay);
    }
}
