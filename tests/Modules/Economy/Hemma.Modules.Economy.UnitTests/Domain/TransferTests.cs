using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class TransferTests
{
    [Fact]
    public void Create_WhenLegsReconcile_ReturnsTransfer()
    {
        var householdId = Guid.NewGuid();
        var amount = Money.Create(5000, "SEK").Value;
        var from = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;
        var to = Account.Create(householdId, "Savings", AccountType.Savings, Money.Create(0, "SEK").Value).Value;
        var transferId = TransferId.New();
        var outflow = Transaction.CreateTransferLeg(householdId, from, null, amount, new DateOnly(2026, 6, 5), null, null, transferId, isOutflow: true).Value;
        var inflow = Transaction.CreateTransferLeg(householdId, to, null, amount, new DateOnly(2026, 6, 5), null, null, transferId, isOutflow: false).Value;

        var transfer = Transfer.Create(householdId, outflow, inflow, TransferMode.Savings);

        Assert.False(transfer.IsError);
        Assert.Equal(outflow.Id, transfer.Value.OutflowTransactionId);
        Assert.Equal(inflow.Id, transfer.Value.InflowTransactionId);
    }

    [Fact]
    public void Create_WhenAmountsDiffer_ReturnsError()
    {
        var householdId = Guid.NewGuid();
        var from = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;
        var to = Account.Create(householdId, "Savings", AccountType.Savings, Money.Create(0, "SEK").Value).Value;
        var transferId = TransferId.New();
        var outflow = Transaction.CreateTransferLeg(householdId, from, null, Money.Create(5000, "SEK").Value, new DateOnly(2026, 6, 5), null, null, transferId, isOutflow: true).Value;
        var inflow = Transaction.CreateTransferLeg(householdId, to, null, Money.Create(4000, "SEK").Value, new DateOnly(2026, 6, 5), null, null, transferId, isOutflow: false).Value;

        var transfer = Transfer.Create(householdId, outflow, inflow, TransferMode.Neutral);

        Assert.True(transfer.IsError);
    }
}
