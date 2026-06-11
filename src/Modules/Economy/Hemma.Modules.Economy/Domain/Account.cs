using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class Account : AggregateRoot<AccountId>
{
    private Account(AccountId id, Guid householdId, string name, AccountType type, Money openingBalance) : base(id)
    {
        HouseholdId = householdId;
        Name = name;
        Type = type;
        OpeningBalance = openingBalance;
    }

    private Account() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Name { get; private set; } = null!;
    public AccountType Type { get; private set; } = null!;
    public Money OpeningBalance { get; private set; } = null!;

    public static ErrorOr<Account> Create(Guid householdId, string name, AccountType type, Money openingBalance)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName.IsError)
        {
            return normalizedName.Errors;
        }

        return new Account(AccountId.New(), householdId, normalizedName.Value, type, openingBalance);
    }

    private static ErrorOr<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return EconomyErrors.AccountNameInvalid;
        }

        var trimmed = name.Trim();
        return trimmed.Length <= 100
            ? trimmed
            : EconomyErrors.AccountNameInvalid;
    }
}
