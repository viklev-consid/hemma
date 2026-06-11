using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record AccountType
{
    public static readonly AccountType Spending = new("Spending");
    public static readonly AccountType Savings = new("Savings");

    private static readonly Dictionary<string, AccountType> known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Spending.Name] = Spending,
            [Savings.Name] = Savings
        };

    private AccountType(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static ErrorOr<AccountType> Create(string name) =>
        known.TryGetValue(name.Trim(), out var type)
            ? type
            : EconomyErrors.AccountTypeInvalid;

    public override string ToString() => Name;
}
