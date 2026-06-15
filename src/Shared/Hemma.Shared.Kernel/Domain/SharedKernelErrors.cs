using ErrorOr;

namespace Hemma.Shared.Kernel.Domain;

public static class SharedKernelErrors
{
    // Keep the original Economy.Money.* error codes because clients may key off them.
    public static readonly Error CurrencyInvalid =
        Error.Validation("Economy.Money.CurrencyInvalid", "Currency must be SEK.");

    public static readonly Error AmountNegative =
        Error.Validation("Economy.Money.AmountNegative", "Money amount cannot be negative.");

    public static readonly Error CurrencyMismatch =
        Error.Validation("Economy.Money.CurrencyMismatch", "Money values must use the same currency.");
}
