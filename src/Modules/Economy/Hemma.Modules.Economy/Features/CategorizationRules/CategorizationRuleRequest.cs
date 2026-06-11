namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed record CategorizationRuleRequest(Guid HouseholdId, string Match, string Pattern, Guid TargetCategoryId);
