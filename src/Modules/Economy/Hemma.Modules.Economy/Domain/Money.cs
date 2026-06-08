using ErrorOr;
using Hemma.Modules.Economy.Errors;

namespace Hemma.Modules.Economy.Domain;

public sealed record Money
{
    public const string SupportedCurrency = "SEK";

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
        if (!string.Equals(normalizedCurrency, SupportedCurrency, StringComparison.Ordinal))
        {
            return EconomyErrors.CurrencyInvalid;
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), normalizedCurrency);
    }

    public ErrorOr<Money> Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            return EconomyErrors.CurrencyMismatch;
        }

        return Create(Amount + other.Amount, Currency);
    }
}
