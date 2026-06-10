using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class TransactionTests
{
    [Fact]
    public void LinkToSubscription_WhenAlreadyLinkedToSameSubscription_IsIdempotent()
    {
        var transaction = CreateTransaction();
        var subscriptionId = Guid.NewGuid();

        var first = transaction.LinkToSubscription(subscriptionId);
        var second = transaction.LinkToSubscription(subscriptionId);

        Assert.False(first.IsError);
        Assert.False(second.IsError);
        Assert.Equal(subscriptionId, transaction.SubscriptionId);
    }

    [Fact]
    public void LinkToSubscription_WhenLinkedToAnotherSubscription_ReturnsConflict()
    {
        var transaction = CreateTransaction();
        var firstSubscriptionId = Guid.NewGuid();

        var first = transaction.LinkToSubscription(firstSubscriptionId);
        var second = transaction.LinkToSubscription(Guid.NewGuid());

        Assert.False(first.IsError);
        Assert.True(second.IsError);
        Assert.Equal(ErrorOr.ErrorType.Conflict, second.FirstError.Type);
        Assert.Equal(firstSubscriptionId, transaction.SubscriptionId);
    }

    [Fact]
    public void LinkToSubscription_AfterUnlink_LinksToNewSubscription()
    {
        var transaction = CreateTransaction();
        var firstSubscriptionId = Guid.NewGuid();
        var secondSubscriptionId = Guid.NewGuid();

        transaction.LinkToSubscription(firstSubscriptionId);
        var unlinked = transaction.UnlinkSubscription(firstSubscriptionId);
        var relinked = transaction.LinkToSubscription(secondSubscriptionId);

        Assert.False(unlinked.IsError);
        Assert.False(relinked.IsError);
        Assert.Equal(secondSubscriptionId, transaction.SubscriptionId);
    }

    private static Transaction CreateTransaction()
    {
        var householdId = Guid.NewGuid();
        var account = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;

        return Transaction.Record(
            householdId,
            account,
            category: null,
            Money.Create(119, "SEK").Value,
            new DateOnly(2026, 3, 15),
            "Spotify",
            TransactionKind.Expense,
            payerId: null).Value;
    }
}
