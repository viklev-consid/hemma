namespace Hemma.Modules.Economy.Domain;

public readonly record struct CategorizationRuleId(Guid Value)
{
    public static CategorizationRuleId New() => new(Guid.NewGuid());
}
