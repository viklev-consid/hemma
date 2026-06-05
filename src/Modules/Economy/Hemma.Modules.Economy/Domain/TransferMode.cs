using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record TransferMode
{
    public static readonly TransferMode Neutral = new("Neutral");
    public static readonly TransferMode Savings = new("Savings");

    private static readonly Dictionary<string, TransferMode> known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Neutral.Name] = Neutral,
            [Savings.Name] = Savings
        };

    private TransferMode(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static ErrorOr<TransferMode> Create(string name) =>
        known.TryGetValue(name.Trim(), out var mode)
            ? mode
            : EconomyErrors.TransferModeInvalid;

    public override string ToString() => Name;
}
