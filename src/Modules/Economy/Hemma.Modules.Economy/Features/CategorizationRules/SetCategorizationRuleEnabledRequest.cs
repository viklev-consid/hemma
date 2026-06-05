namespace Hemma.Modules.Economy.Features.CategorizationRules;

public sealed record SetCategorizationRuleEnabledRequest(Guid HouseholdId, bool Enabled);
