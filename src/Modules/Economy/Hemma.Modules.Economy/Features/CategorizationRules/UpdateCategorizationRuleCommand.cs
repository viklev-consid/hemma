namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed record UpdateCategorizationRuleCommand(Guid RuleId, Guid HouseholdId, string Match, string Pattern, Guid TargetCategoryId);
