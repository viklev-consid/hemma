namespace Hemma.Modules.Economy.Features.Contracts;

internal static class MoneyContract
{
    public static MoneyDto From(Money money) => new(money.Amount, money.Currency);
}
