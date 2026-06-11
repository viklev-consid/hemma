using System.Text.RegularExpressions;
using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class CategorizationRule : AggregateRoot<CategorizationRuleId>
{
    private CategorizationRule(
        CategorizationRuleId id,
        Guid householdId,
        CategorizationRuleMatch match,
        string pattern,
        CategoryId targetCategoryId,
        bool enabled) : base(id)
    {
        HouseholdId = householdId;
        Match = match;
        Pattern = pattern;
        TargetCategoryId = targetCategoryId;
        Enabled = enabled;
    }

    private CategorizationRule() : base(default) { }

    public Guid HouseholdId { get; private set; }
    public CategorizationRuleMatch Match { get; private set; } = null!;
    public string Pattern { get; private set; } = string.Empty;
    public CategoryId TargetCategoryId { get; private set; } = null!;
    public bool Enabled { get; private set; }

    public static ErrorOr<CategorizationRule> Create(
        Guid householdId,
        CategorizationRuleMatch match,
        string pattern,
        Category category)
    {
        var normalizedPattern = NormalizePattern(pattern);
        if (normalizedPattern is null)
        {
            return EconomyErrors.CategorizationRulePatternInvalid;
        }

        if (category.HouseholdId != householdId)
        {
            return EconomyErrors.CategoryNotFound;
        }

        if (match == CategorizationRuleMatch.Regex && !IsValidRegex(normalizedPattern))
        {
            return EconomyErrors.CategorizationRulePatternInvalid;
        }

        return new CategorizationRule(
            CategorizationRuleId.New(),
            householdId,
            match,
            normalizedPattern,
            category.Id,
            enabled: true);
    }

    public ErrorOr<Success> Update(CategorizationRuleMatch match, string pattern, Category category)
    {
        var normalizedPattern = NormalizePattern(pattern);
        if (normalizedPattern is null)
        {
            return EconomyErrors.CategorizationRulePatternInvalid;
        }

        if (category.HouseholdId != HouseholdId)
        {
            return EconomyErrors.CategoryNotFound;
        }

        if (match == CategorizationRuleMatch.Regex && !IsValidRegex(normalizedPattern))
        {
            return EconomyErrors.CategorizationRulePatternInvalid;
        }

        Match = match;
        Pattern = normalizedPattern;
        TargetCategoryId = category.Id;
        return Result.Success;
    }

    public void SetEnabled(bool enabled) => Enabled = enabled;

    public bool Matches(string description)
    {
        if (!Enabled)
        {
            return false;
        }

        if (Match == CategorizationRuleMatch.Contains)
        {
            return description.Contains(Pattern, StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            return Regex.IsMatch(description, Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static string? NormalizePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        var trimmed = pattern.Trim();
        return trimmed.Length <= 200 ? trimmed : null;
    }

    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
