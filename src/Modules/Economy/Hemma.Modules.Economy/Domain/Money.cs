using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static ErrorOr<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            return EconomyErrors.AmountNegative;
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3 || normalizedCurrency.Any(c => c is < 'A' or > 'Z'))
        {
            return EconomyErrors.CurrencyInvalid;
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), normalizedCurrency);
    }

    public Money Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot add money values with different currencies.");
        }

        return new Money(Amount + other.Amount, Currency);
    }
}
