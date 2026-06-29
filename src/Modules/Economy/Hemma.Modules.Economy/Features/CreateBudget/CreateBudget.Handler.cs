using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.CreateBudget;

public sealed class CreateBudgetHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<CreateBudgetResult>> Handle(CreateBudgetCommand cmd, CancellationToken ct)
    {
        var settings = await db.EconomySettings
            .SingleOrDefaultAsync(value => value.HouseholdId == cmd.HouseholdId, ct);
        if (settings is null)
        {
            return EconomyErrors.SettingsNotFound;
        }

        var period = settings.GetPeriodContaining(cmd.AnchorDate);
        var existingBudget = await db.Budgets
            .AsNoTracking()
            .Include(budget => budget.Lines)
            .SingleOrDefaultAsync(budget => budget.HouseholdId == cmd.HouseholdId && budget.PeriodStartsOn == period.StartsOn, ct);
        if (existingBudget is not null)
        {
            return new CreateBudgetResult(
                BudgetResponse.From(existingBudget, GetPaceStartsOn(settings.CreatedOn, period)),
                Created: false);
        }

        var budget = Budget.Create(cmd.HouseholdId, period);
        db.Budgets.Add(budget);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(budget.HouseholdId, "economy.budget.created", "Budget", budget.Id.Value, null, ct);

        return new CreateBudgetResult(
            BudgetResponse.From(budget, GetPaceStartsOn(settings.CreatedOn, period)),
            Created: true);
    }

    private static DateOnly GetPaceStartsOn(DateOnly settingsCreatedOn, BudgetPeriod period) =>
        settingsCreatedOn >= period.StartsOn && settingsCreatedOn <= period.EndsOn
            ? settingsCreatedOn
            : period.StartsOn;
}
