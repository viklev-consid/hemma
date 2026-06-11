using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;

public sealed class CopyBudgetFromPreviousPeriodHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<BudgetResponse>> Handle(CopyBudgetFromPreviousPeriodCommand cmd, CancellationToken ct)
    {
        var settings = await db.EconomySettings
            .SingleOrDefaultAsync(value => value.HouseholdId == cmd.HouseholdId, ct);
        if (settings is null)
        {
            return EconomyErrors.SettingsNotFound;
        }

        var targetPeriod = settings.GetPeriodContaining(cmd.AnchorDate);
        var target = await db.Budgets
            .Include(value => value.Lines)
            .SingleOrDefaultAsync(value => value.HouseholdId == cmd.HouseholdId && value.PeriodStartsOn == targetPeriod.StartsOn, ct);
        if (target is null)
        {
            target = Budget.Create(cmd.HouseholdId, targetPeriod);
            db.Budgets.Add(target);
        }

        var previousPeriod = targetPeriod.Previous();
        var previous = await db.Budgets
            .AsNoTracking()
            .Include(value => value.Lines)
            .SingleOrDefaultAsync(value => value.HouseholdId == cmd.HouseholdId && value.PeriodStartsOn == previousPeriod.StartsOn, ct);
        if (previous is not null)
        {
            var categoryIds = previous.Lines.Select(line => line.CategoryId).ToArray();
            var categories = await db.Categories
                .Where(category => category.HouseholdId == cmd.HouseholdId && categoryIds.Contains(category.Id))
                .ToDictionaryAsync(category => category.Id, ct);

            foreach (var sourceLine in previous.Lines)
            {
                if (categories.TryGetValue(sourceLine.CategoryId, out var category))
                {
                    var upsert = target.UpsertLine(category, sourceLine.Amount);
                    if (upsert.IsError)
                    {
                        return upsert.Errors;
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(target.HouseholdId, "economy.budget.copied_from_previous", "Budget", target.Id.Value, null, ct);

        return BudgetResponse.From(target, GetPaceStartsOn(settings.CreatedOn, targetPeriod));
    }

    private static DateOnly GetPaceStartsOn(DateOnly settingsCreatedOn, BudgetPeriod period) =>
        settingsCreatedOn >= period.StartsOn && settingsCreatedOn <= period.EndsOn
            ? settingsCreatedOn
            : period.StartsOn;
}
