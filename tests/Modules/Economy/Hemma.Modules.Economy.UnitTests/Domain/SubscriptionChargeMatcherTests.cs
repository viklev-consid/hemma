using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class SubscriptionChargeMatcherTests
{
    [Fact]
    public void Match_WhenDescriptionDayAndAmountAlign_ReturnsMatch()
    {
        var subscription = CreateSubscription();

        var match = SubscriptionChargeMatcher.Match(subscription, new DateOnly(2026, 3, 13), 119, "Spotify AB");

        Assert.NotNull(match);
        Assert.Equal(2, match.DayDelta);
        Assert.Equal(0, match.AmountDelta);
    }

    [Theory]
    [InlineData("Netflix")]
    [InlineData(null)]
    [InlineData("")]
    public void Match_WhenDescriptionDoesNotContainName_ReturnsNull(string? description)
    {
        var subscription = CreateSubscription();

        Assert.Null(SubscriptionChargeMatcher.Match(subscription, new DateOnly(2026, 3, 15), 119, description));
    }

    [Fact]
    public void Match_WhenDayDeltaExceedsThreshold_ReturnsNull()
    {
        var subscription = CreateSubscription();

        Assert.Null(SubscriptionChargeMatcher.Match(subscription, new DateOnly(2026, 3, 11), 119, "Spotify"));
    }

    [Fact]
    public void Match_WhenAmountOutsideTolerance_ReturnsNull()
    {
        var subscription = CreateSubscription();

        Assert.Null(SubscriptionChargeMatcher.Match(subscription, new DateOnly(2026, 3, 15), 200, "Spotify"));
    }

    [Fact]
    public void Match_WhenCadenceDoesNotChargeInMonth_ReturnsNull()
    {
        var subscription = CreateSubscription(intervalMonths: 2);

        Assert.Null(SubscriptionChargeMatcher.Match(subscription, new DateOnly(2026, 2, 15), 119, "Spotify"));
    }

    private static Subscription CreateSubscription(int intervalMonths = 1) =>
        Subscription.Create(
            Guid.NewGuid(),
            "Spotify",
            SubscriptionCadence.Create("Monthly", intervalMonths, 15).Value,
            Money.Create(119, "SEK").Value,
            SubscriptionLifecycleState.Active,
            trialEndsOn: null,
            account: null,
            new DateOnly(2026, 1, 15)).Value;
}
