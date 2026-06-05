namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed record DeleteCategorizationRuleCommand(Guid RuleId, Guid HouseholdId);
