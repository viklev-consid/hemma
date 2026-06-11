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

    [Fact]
    public void Create_NonTrialSubscriptionWithTrialEnd_DiscardsTrialEnd()
    {
        var householdId = Guid.NewGuid();

        var subscription = Subscription.Create(
            householdId,
            "Spotify",
            SubscriptionCadence.Create("Monthly", 1, 15).Value,
            Money.Create(119, "SEK").Value,
            SubscriptionLifecycleState.Active,
            new DateOnly(2026, 6, 30),
            account: null,
            new DateOnly(2026, 1, 15));

        Assert.False(subscription.IsError);
        Assert.Null(subscription.Value.TrialEndsOn);
    }

    [Fact]
    public void Create_CancelledSubscription_ReturnsValidationFailure()
    {
        var householdId = Guid.NewGuid();

        var subscription = Subscription.Create(
            householdId,
            "Cancelled",
            SubscriptionCadence.Create("Monthly", 1, 15).Value,
            Money.Create(119, "SEK").Value,
            SubscriptionLifecycleState.Cancelled,
            trialEndsOn: null,
            account: null,
            new DateOnly(2026, 1, 15));

        Assert.True(subscription.IsError);
    }

    [Fact]
    public void ChangeLifecycleState_WhenCancelled_DoesNotReactivate()
    {
        var householdId = Guid.NewGuid();
        var subscription = Subscription.Create(
            householdId,
            "Spotify",
            SubscriptionCadence.Create("Monthly", 1, 15).Value,
            Money.Create(119, "SEK").Value,
            SubscriptionLifecycleState.Active,
            trialEndsOn: null,
            account: null,
            new DateOnly(2026, 1, 15)).Value;

        var cancelled = subscription.ChangeLifecycleState(SubscriptionLifecycleState.Cancelled, trialEndsOn: null, new DateOnly(2026, 3, 1));
        var reactivated = subscription.ChangeLifecycleState(SubscriptionLifecycleState.Active, trialEndsOn: null, new DateOnly(2026, 3, 2));

        Assert.False(cancelled.IsError);
        Assert.True(reactivated.IsError);
        Assert.Equal(SubscriptionLifecycleState.Cancelled, subscription.LifecycleState);
        Assert.Equal(new DateOnly(2026, 3, 1), subscription.CancelledOn);
    }

    [Fact]
    public void ChangeLifecycleState_WhenNotCancelling_DoesNotSetCancelledOn()
    {
        var householdId = Guid.NewGuid();
        var subscription = Subscription.Create(
            householdId,
            "Spotify",
            SubscriptionCadence.Create("Monthly", 1, 15).Value,
            Money.Create(119, "SEK").Value,
            SubscriptionLifecycleState.Active,
            trialEndsOn: null,
            account: null,
            new DateOnly(2026, 1, 15)).Value;

        var paused = subscription.ChangeLifecycleState(SubscriptionLifecycleState.Paused, trialEndsOn: null, new DateOnly(2026, 3, 1));

        Assert.False(paused.IsError);
        Assert.Null(subscription.CancelledOn);
    }

    [Fact]
    public void TrialReminder_WhenMarkedForTrialEnd_DoesNotSendAgain()
    {
        var householdId = Guid.NewGuid();
        var subscription = Subscription.Create(
            householdId,
            "Trial",
            SubscriptionCadence.Create("Monthly", 1, 15).Value,
            Money.Create(119, "SEK").Value,
            SubscriptionLifecycleState.Trial,
            new DateOnly(2026, 6, 10),
            account: null,
            new DateOnly(2026, 6, 1)).Value;

        Assert.True(subscription.ShouldSendTrialReminder());

        subscription.MarkTrialReminderSent();

        Assert.False(subscription.ShouldSendTrialReminder());
    }
}
