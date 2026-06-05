using Hemma.Modules.Economy.Domain;

namespace Hemma.Modules.Economy.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class RecurringBillTests
{
    [Fact]
    public void Create_MonthlyBill_CalculatesNextDueDate()
    {
        var householdId = Guid.NewGuid();
        var account = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;
        var cadence = RecurringBillCadence.Create("Monthly", 1, 15).Value;

        var bill = RecurringBill.Create(
            householdId,
            "Rent",
            account,
            null,
            Money.Create(119, "SEK").Value,
            cadence,
            RecurringBillType.Fixed,
            RecurringBillDirection.Expense,
            new DateOnly(2026, 6, 5),
            null);

        Assert.False(bill.IsError);
        Assert.Equal(new DateOnly(2026, 6, 15), bill.Value.NextDueOn);
    }

    [Fact]
    public void SkipOccurrence_AdvancesOnlyOneCycle()
    {
        var householdId = Guid.NewGuid();
        var account = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;
        var bill = RecurringBill.Create(
            householdId,
            "Rent",
            account,
            null,
            Money.Create(119, "SEK").Value,
            RecurringBillCadence.Create("Monthly", 1, 15).Value,
            RecurringBillType.Fixed,
            RecurringBillDirection.Expense,
            new DateOnly(2026, 6, 1),
            null).Value;

        var skipped = bill.MarkSkipped(new DateOnly(2026, 6, 15));

        Assert.False(skipped.IsError);
        Assert.Equal(new DateOnly(2026, 7, 15), bill.NextDueOn);
        Assert.Single(bill.Occurrences);
    }

    [Fact]
    public void EstimatedDue_CreatesPendingTransaction()
    {
        var householdId = Guid.NewGuid();
        var account = Account.Create(householdId, "Checking", AccountType.Spending, Money.Create(0, "SEK").Value).Value;
        var bill = RecurringBill.Create(
            householdId,
            "Electricity",
            account,
            null,
            Money.Create(500, "SEK").Value,
            RecurringBillCadence.Create("Monthly", 1, 15).Value,
            RecurringBillType.Estimated,
            RecurringBillDirection.Expense,
            new DateOnly(2026, 6, 1),
            null).Value;

        var pending = bill.CreatePending(account, null, new DateOnly(2026, 6, 15));

        Assert.False(pending.IsError);
        Assert.True(pending.Value.IsPending);
        Assert.Equal(new DateOnly(2026, 7, 15), bill.NextDueOn);
    }
}
