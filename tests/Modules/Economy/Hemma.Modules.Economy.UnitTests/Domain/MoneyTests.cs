using Hemma.Modules.Economy.Domain;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class MoneyTests
{
    [Fact]
    public void Create_WhenAmountIsNegative_ReturnsError()
    {
        var result = Money.Create(-1, "SEK");

        Assert.True(result.IsError);
    }

    [Fact]
    public void Add_WhenCurrencyMatches_ReturnsSum()
    {
        var left = Money.Create(10.25m, "sek").Value;
        var right = Money.Create(2.50m, "SEK").Value;

        var sum = left.Add(right);

        Assert.False(sum.IsError);
        Assert.Equal(12.75m, sum.Value.Amount);
        Assert.Equal("SEK", sum.Value.Currency);
    }

    [Fact]
    public void Create_WhenCurrencyIsNotSek_ReturnsError()
    {
        var eur = Money.Create(10, "EUR");

        Assert.True(eur.IsError);
    }
}
