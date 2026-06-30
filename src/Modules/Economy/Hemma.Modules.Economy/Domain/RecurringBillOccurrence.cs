namespace Hemma.Modules.Economy.Domain;

public sealed class RecurringBillOccurrence
{
    private RecurringBillOccurrence(
        Guid id,
        RecurringBillId recurringBillId,
        DateOnly dueOn,
        RecurringBillOccurrenceState state,
        TransactionId? transactionId)
    {
        Id = id;
        RecurringBillId = recurringBillId;
        DueOn = dueOn;
        State = state;
        TransactionId = transactionId;
    }

    private RecurringBillOccurrence() { }

    public Guid Id { get; private set; }
    public RecurringBillId RecurringBillId { get; private set; }
    public DateOnly DueOn { get; private set; }
    public RecurringBillOccurrenceState State { get; private set; } = null!;
    public TransactionId? TransactionId { get; private set; }

    public static RecurringBillOccurrence Pending(RecurringBillId recurringBillId, DateOnly dueOn) =>
        new(Guid.NewGuid(), recurringBillId, dueOn, RecurringBillOccurrenceState.Pending, null);

    public static RecurringBillOccurrence Pending(RecurringBillId recurringBillId, DateOnly dueOn, TransactionId transactionId) =>
        new(Guid.NewGuid(), recurringBillId, dueOn, RecurringBillOccurrenceState.Pending, transactionId);

    public static RecurringBillOccurrence Posted(RecurringBillId recurringBillId, DateOnly dueOn, TransactionId transactionId) =>
        new(Guid.NewGuid(), recurringBillId, dueOn, RecurringBillOccurrenceState.Posted, transactionId);

    public static RecurringBillOccurrence Skipped(RecurringBillId recurringBillId, DateOnly dueOn) =>
        new(Guid.NewGuid(), recurringBillId, dueOn, RecurringBillOccurrenceState.Skipped, null);

    public static RecurringBillOccurrence Paused(RecurringBillId recurringBillId, DateOnly dueOn) =>
        new(Guid.NewGuid(), recurringBillId, dueOn, RecurringBillOccurrenceState.Paused, null);

    public void Confirm(TransactionId transactionId)
    {
        State = RecurringBillOccurrenceState.Confirmed;
        TransactionId = transactionId;
    }

    public void Resume()
    {
        if (State == RecurringBillOccurrenceState.Paused)
        {
            State = RecurringBillOccurrenceState.Skipped;
        }
    }
}
