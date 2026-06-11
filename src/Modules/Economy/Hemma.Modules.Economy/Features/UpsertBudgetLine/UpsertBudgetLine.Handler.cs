using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Contracts;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.UpsertBudgetLine;

public sealed class UpsertBudgetLineHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<BudgetResponse>> Handle(UpsertBudgetLineCommand cmd, CancellationToken ct)
    {
        var settings = await db.EconomySettings
            .SingleOrDefaultAsync(value => value.HouseholdId == cmd.HouseholdId, ct);
        if (settings is null)
        {
            return EconomyErrors.SettingsNotFound;
        }

        var budgetId = new BudgetId(cmd.BudgetId);
        var budget = await db.Budgets
            .Include(value => value.Lines)
            .SingleOrDefaultAsync(value => value.HouseholdId == cmd.HouseholdId && value.Id == budgetId, ct);
        if (budget is null)
        {
            return EconomyErrors.BudgetNotFound;
        }

        var categoryId = new CategoryId(cmd.CategoryId);
        var category = await db.Categories
            .SingleOrDefaultAsync(value => value.HouseholdId == cmd.HouseholdId && value.Id == categoryId, ct);
        if (category is null)
        {
            return EconomyErrors.CategoryNotFound;
        }

        var amount = Money.Create(cmd.Amount, cmd.Currency);
        if (amount.IsError)
        {
            return amount.Errors;
        }

        if (!string.Equals(amount.Value.Currency, settings.DefaultCurrency, StringComparison.Ordinal))
        {
            return EconomyErrors.CurrencyMismatch;
        }

        var upsert = budget.UpsertLine(category, amount.Value);
        if (upsert.IsError)
        {
            return upsert.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(budget.HouseholdId, "economy.budget_line.upserted", "BudgetLine", upsert.Value.Id.Value, null, ct);

        var period = new BudgetPeriod(budget.PeriodStartsOn, budget.PeriodEndsOn);
        return BudgetResponse.From(budget, GetPaceStartsOn(settings.CreatedOn, period));
    }

    private static DateOnly GetPaceStartsOn(DateOnly settingsCreatedOn, BudgetPeriod period) =>
        settingsCreatedOn >= period.StartsOn && settingsCreatedOn <= period.EndsOn
            ? settingsCreatedOn
            : period.StartsOn;
}
