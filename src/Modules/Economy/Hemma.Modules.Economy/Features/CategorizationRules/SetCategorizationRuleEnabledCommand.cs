namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed record SetCategorizationRuleEnabledCommand(Guid RuleId, Guid HouseholdId, bool Enabled);
