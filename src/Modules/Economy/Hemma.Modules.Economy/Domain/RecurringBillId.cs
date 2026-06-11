namespace Hemma.Modules.Economy.Domain;

public readonly record struct RecurringBillId(Guid Value)
{
    public static RecurringBillId New() => new(Guid.NewGuid());
}
