using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed record CategorizationRuleResponse(
    Guid CategorizationRuleId,
    Guid HouseholdId,
    string Match,
    string Pattern,
    Guid TargetCategoryId,
    bool Enabled)
{
    public static CategorizationRuleResponse From(CategorizationRule rule) =>
        new(
            rule.Id.Value,
            rule.HouseholdId,
            rule.Match.Name,
            rule.Pattern,
            rule.TargetCategoryId.Value,
            rule.Enabled);
}
