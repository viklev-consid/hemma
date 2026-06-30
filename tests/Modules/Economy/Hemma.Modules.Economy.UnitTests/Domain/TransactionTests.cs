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

    [Fact]
    public void UpdateDetails_ChangesEditableFieldsAndPreservesLinks()
    {
        var householdId = Guid.NewGuid();
        var originalAccount = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;
        var newAccount = Account.Create(householdId, "Savings", AccountType.Savings, Money.Create(0, "SEK").Value).Value;
        var category = Category.Create(householdId, "Salary", null, budgetable: false).Value;
        var transaction = Transaction.Record(
            householdId,
            originalAccount,
            category: null,
            Money.Create(119, "SEK").Value,
            new DateOnly(2026, 3, 15),
            "Spotify",
            TransactionKind.Expense,
            payerId: null).Value;
        var subscriptionId = Guid.NewGuid();
        transaction.LinkToSubscription(subscriptionId);
        transaction.AssignToProject(Guid.NewGuid());
        transaction.AttachReceipt("receipts", "receipt.pdf");
        var payerId = Guid.NewGuid();

        var result = transaction.UpdateDetails(
            newAccount,
            category,
            Money.Create(2500, "SEK").Value,
            new DateOnly(2026, 6, 25),
            "  Paycheck  ",
            TransactionKind.Income,
            payerId);

        Assert.False(result.IsError);
        Assert.Equal(newAccount.Id, transaction.AccountId);
        Assert.Equal(category.Id, transaction.CategoryId);
        Assert.Equal(2500, transaction.Amount.Amount);
        Assert.Equal(new DateOnly(2026, 6, 25), transaction.OccurredOn);
        Assert.Equal("Paycheck", transaction.Note);
        Assert.Equal(TransactionKind.Income, transaction.Kind);
        Assert.Equal(payerId, transaction.PayerId);
        Assert.Equal(subscriptionId, transaction.SubscriptionId);
        Assert.True(transaction.HasReceipt);
        Assert.NotNull(transaction.ProjectId);
    }

    [Fact]
    public void UpdateDetails_WhenTransferKind_ReturnsConflict()
    {
        var householdId = Guid.NewGuid();
        var account = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;
        var transaction = Transaction.Record(
            householdId,
            account,
            category: null,
            Money.Create(119, "SEK").Value,
            new DateOnly(2026, 3, 15),
            "Spotify",
            TransactionKind.Expense,
            payerId: null).Value;

        var result = transaction.UpdateDetails(
            account,
            category: null,
            Money.Create(119, "SEK").Value,
            new DateOnly(2026, 3, 15),
            "Spotify",
            TransactionKind.Transfer,
            payerId: null);

        Assert.True(result.IsError);
        Assert.Equal(ErrorOr.ErrorType.Conflict, result.FirstError.Type);
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
