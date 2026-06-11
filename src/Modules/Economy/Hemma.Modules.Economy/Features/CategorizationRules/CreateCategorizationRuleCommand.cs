namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed record CreateCategorizationRuleCommand(Guid HouseholdId, string Match, string Pattern, Guid TargetCategoryId);
