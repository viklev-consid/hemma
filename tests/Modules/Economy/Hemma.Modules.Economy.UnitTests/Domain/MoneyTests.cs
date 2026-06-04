using Hemma.Modules.Economy.Domain;

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

        Assert.Equal(12.75m, sum.Amount);
        Assert.Equal("SEK", sum.Currency);
    }

    [Fact]
    public void Add_WhenCurrencyDiffers_Throws()
    {
        var sek = Money.Create(10, "SEK").Value;
        var eur = Money.Create(10, "EUR").Value;

        Assert.Throws<InvalidOperationException>(() => sek.Add(eur));
    }
}
