using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class SubscriptionTests
{
    [Fact]
    public void Create_ActiveSubscription_DoesNotCreateTransactions()
    {
        var householdId = Guid.NewGuid();
        var account = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;

        var subscription = Subscription.Create(
            householdId,
            "Spotify",
            SubscriptionCadence.Create("Monthly", 1, 15).Value,
            Money.Create(119, "SEK").Value,
            SubscriptionLifecycleState.Active,
            trialEndsOn: null,
            account,
            new DateOnly(2026, 1, 15));

        Assert.False(subscription.IsError);
        Assert.Equal("Spotify", subscription.Value.Name);
        Assert.Equal(SubscriptionLifecycleState.Active, subscription.Value.LifecycleState);
    }

    [Fact]
    public void Create_TrialSubscriptionWithoutTrialEnd_ReturnsValidationFailure()
    {
        var householdId = Guid.NewGuid();

        var subscription = Subscription.Create(
            householdId,
            "Trial",
            SubscriptionCadence.Create("Monthly", 1, 15).Value,
            Money.Create(119, "SEK").Value,
            SubscriptionLifecycleState.Trial,
            trialEndsOn: null,
            account: null,
            new DateOnly(2026, 1, 15));

        Assert.True(subscription.IsError);
    }
}
