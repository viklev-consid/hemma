using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

public sealed class CategorizationRuleTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void ContainsRule_MatchesCaseInsensitiveDescription()
    {
        var householdId = Guid.NewGuid();
        var category = Category.Create(householdId, "Food", null, true).Value;
        var rule = CategorizationRule.Create(householdId, CategorizationRuleMatch.Contains, "ICA", category).Value;

        Assert.True(rule.Matches("weekly shop at ica kvantum"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegexRule_WithInvalidPattern_ReturnsValidationFailure()
    {
        var householdId = Guid.NewGuid();
        var category = Category.Create(householdId, "Food", null, true).Value;

        var rule = CategorizationRule.Create(householdId, CategorizationRuleMatch.Regex, "[", category);

        Assert.True(rule.IsError);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DisabledRule_DoesNotMatch()
    {
        var householdId = Guid.NewGuid();
        var category = Category.Create(householdId, "Food", null, true).Value;
        var rule = CategorizationRule.Create(householdId, CategorizationRuleMatch.Contains, "ICA", category).Value;

        rule.SetEnabled(false);

        Assert.False(rule.Matches("ICA"));
    }
}
