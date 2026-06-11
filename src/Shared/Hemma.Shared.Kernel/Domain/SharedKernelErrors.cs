using ErrorOr;

namespace Hemma.Shared.Kernel.Domain;

public static class SharedKernelErrors
{
    public static readonly Error CurrencyInvalid =
        Error.Validation("Shared.Money.CurrencyInvalid", "Currency must be SEK.");

    public static readonly Error AmountNegative =
        Error.Validation("Shared.Money.AmountNegative", "Money amount cannot be negative.");

    public static readonly Error CurrencyMismatch =
        Error.Validation("Shared.Money.CurrencyMismatch", "Money values must use the same currency.");
}
