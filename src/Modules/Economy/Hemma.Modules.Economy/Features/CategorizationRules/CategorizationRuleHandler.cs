using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed class CategorizationRuleHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    private const int maxEnabledRulesPerHousehold = 100;

    public async Task<ErrorOr<CategorizationRuleResponse>> Handle(CreateCategorizationRuleCommand cmd, CancellationToken ct)
    {
        if (await CountEnabledRulesAsync(cmd.HouseholdId, ct) >= maxEnabledRulesPerHousehold)
        {
            return EconomyErrors.CategorizationRuleLimitExceeded;
        }

        var category = await db.Categories.SingleOrDefaultAsync(
            x => x.HouseholdId == cmd.HouseholdId && x.Id == new CategoryId(cmd.TargetCategoryId),
            ct);
        if (category is null)
        {
            return EconomyErrors.CategoryNotFound;
        }

        var match = CategorizationRuleMatch.Create(cmd.Match);
        if (match.IsError)
        {
            return match.Errors;
        }

        var rule = CategorizationRule.Create(cmd.HouseholdId, match.Value, cmd.Pattern, category);
        if (rule.IsError)
        {
            return rule.Errors;
        }

        db.CategorizationRules.Add(rule.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(rule.Value.HouseholdId, "economy.categorization_rule.created", "CategorizationRule", rule.Value.Id.Value, null, ct);
        return CategorizationRuleResponse.From(rule.Value);
    }

    public async Task<ErrorOr<CategorizationRuleResponse>> Handle(UpdateCategorizationRuleCommand cmd, CancellationToken ct)
    {
        var rule = await db.CategorizationRules.SingleOrDefaultAsync(
            x => x.HouseholdId == cmd.HouseholdId && x.Id == new CategorizationRuleId(cmd.RuleId),
            ct);
        if (rule is null)
        {
            return EconomyErrors.CategorizationRuleNotFound;
        }

        var category = await db.Categories.SingleOrDefaultAsync(
            x => x.HouseholdId == cmd.HouseholdId && x.Id == new CategoryId(cmd.TargetCategoryId),
            ct);
        if (category is null)
        {
            return EconomyErrors.CategoryNotFound;
        }

        var match = CategorizationRuleMatch.Create(cmd.Match);
        if (match.IsError)
        {
            return match.Errors;
        }

        var updated = rule.Update(match.Value, cmd.Pattern, category);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(rule.HouseholdId, "economy.categorization_rule.updated", "CategorizationRule", rule.Id.Value, null, ct);
        return CategorizationRuleResponse.From(rule);
    }

    public async Task<ErrorOr<CategorizationRuleResponse>> Handle(SetCategorizationRuleEnabledCommand cmd, CancellationToken ct)
    {
        var rule = await db.CategorizationRules.SingleOrDefaultAsync(
            x => x.HouseholdId == cmd.HouseholdId && x.Id == new CategorizationRuleId(cmd.RuleId),
            ct);
        if (rule is null)
        {
            return EconomyErrors.CategorizationRuleNotFound;
        }

        if (cmd.Enabled &&
            !rule.Enabled &&
            await CountEnabledRulesAsync(cmd.HouseholdId, ct) >= maxEnabledRulesPerHousehold)
        {
            return EconomyErrors.CategorizationRuleLimitExceeded;
        }

        rule.SetEnabled(cmd.Enabled);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(rule.HouseholdId, "economy.categorization_rule.enabled_changed", "CategorizationRule", rule.Id.Value, null, ct);
        return CategorizationRuleResponse.From(rule);
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteCategorizationRuleCommand cmd, CancellationToken ct)
    {
        var rule = await db.CategorizationRules.SingleOrDefaultAsync(
            x => x.HouseholdId == cmd.HouseholdId && x.Id == new CategorizationRuleId(cmd.RuleId),
            ct);
        if (rule is null)
        {
            return EconomyErrors.CategorizationRuleNotFound;
        }

        db.CategorizationRules.Remove(rule);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(rule.HouseholdId, "economy.categorization_rule.deleted", "CategorizationRule", rule.Id.Value, null, ct);
        return Result.Deleted;
    }

    public async Task<ListCategorizationRulesResponse> Handle(ListCategorizationRulesQuery query, CancellationToken ct)
    {
        var rules = await db.CategorizationRules
            .AsNoTracking()
            .Where(x => x.HouseholdId == query.HouseholdId)
            .OrderBy(x => x.Pattern)
            .Select(x => CategorizationRuleResponse.From(x))
            .ToListAsync(ct);

        return new ListCategorizationRulesResponse(rules);
    }

    private Task<int> CountEnabledRulesAsync(Guid householdId, CancellationToken ct) =>
        db.CategorizationRules.CountAsync(x => x.HouseholdId == householdId && x.Enabled, ct);
}
