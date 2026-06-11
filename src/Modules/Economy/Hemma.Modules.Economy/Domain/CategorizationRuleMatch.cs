using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record CategorizationRuleMatch
{
    public static readonly CategorizationRuleMatch Contains = new("Contains");
    public static readonly CategorizationRuleMatch Regex = new("Regex");

    private static readonly Dictionary<string, CategorizationRuleMatch> known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Contains.Name] = Contains,
            [Regex.Name] = Regex
        };

    private CategorizationRuleMatch(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static ErrorOr<CategorizationRuleMatch> Create(string name) =>
        !string.IsNullOrWhiteSpace(name) && known.TryGetValue(name.Trim(), out var match)
            ? match
            : EconomyErrors.CategorizationRuleMatchInvalid;

    public override string ToString() => Name;
}
