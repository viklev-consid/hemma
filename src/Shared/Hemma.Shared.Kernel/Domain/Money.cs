using ErrorOr;

namespace Hemma.Shared.Kernel.Domain;

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
            return SharedKernelErrors.AmountNegative;
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (!string.Equals(normalizedCurrency, SupportedCurrency, StringComparison.Ordinal))
        {
            return SharedKernelErrors.CurrencyInvalid;
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), normalizedCurrency);
    }

    public ErrorOr<Money> Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            return SharedKernelErrors.CurrencyMismatch;
        }

        return Create(Amount + other.Amount, Currency);
    }
}
