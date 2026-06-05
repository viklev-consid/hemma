using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed class CategorizationRuleHandler(EconomyDbContext db)
{
    public async Task<ErrorOr<CategorizationRuleResponse>> Handle(CreateCategorizationRuleCommand cmd, CancellationToken ct)
    {
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

        rule.SetEnabled(cmd.Enabled);
        await db.SaveChangesAsync(ct);
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
}
