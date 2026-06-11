using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.CreateEconomySettings;

public sealed class CreateEconomySettingsHandler(EconomyDbContext db, IClock clock, EconomyAuditPublisher audit)
{
    private static readonly string[] starterCategories =
    [
        "Mat",
        "Boende",
        "Transport",
        "Sparande",
        "Personligt"
    ];

    public async Task<ErrorOr<CreateEconomySettingsResponse>> Handle(CreateEconomySettingsCommand cmd, CancellationToken ct)
    {
        if (await db.EconomySettings.AnyAsync(settings => settings.HouseholdId == cmd.HouseholdId, ct))
        {
            return EconomyErrors.SettingsAlreadyExist;
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var settingsResult = EconomySettings.Create(cmd.HouseholdId, cmd.CycleStartDay, cmd.DefaultCurrency, today);
        if (settingsResult.IsError)
        {
            return settingsResult.Errors;
        }

        var settings = settingsResult.Value;
        db.EconomySettings.Add(settings);

        foreach (var name in starterCategories)
        {
            var category = Category.Create(cmd.HouseholdId, name, parentCategory: null, budgetable: true).Value;
            db.Categories.Add(category);
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(settings.HouseholdId, "economy.settings.created", "EconomySettings", settings.Id.Value, null, ct);

        return new CreateEconomySettingsResponse(
            settings.Id.Value,
            settings.HouseholdId,
            settings.CycleStartDay,
            settings.DefaultCurrency,
            settings.CreatedOn);
    }
}
