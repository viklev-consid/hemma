using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class RecurringBill : AggregateRoot<RecurringBillId>
{
    private readonly List<RecurringBillOccurrence> occurrences = [];

    private RecurringBill(
        RecurringBillId id,
        Guid householdId,
        string name,
        AccountId accountId,
        CategoryId? categoryId,
        Money amount,
        RecurringBillCadence cadence,
        RecurringBillType type,
        RecurringBillDirection direction,
        DateOnly startsOn,
        DateOnly nextDueOn,
        string? note) : base(id)
    {
        HouseholdId = householdId;
        Name = name;
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        Cadence = cadence;
        Type = type;
        Direction = direction;
        StartsOn = startsOn;
        NextDueOn = nextDueOn;
        Note = NormalizeNote(note);
    }

    private RecurringBill() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountId AccountId { get; private set; } = null!;
    public CategoryId? CategoryId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public RecurringBillCadence Cadence { get; private set; } = null!;
    public RecurringBillType Type { get; private set; } = null!;
    public RecurringBillDirection Direction { get; private set; } = null!;
    public DateOnly StartsOn { get; private set; }
    public DateOnly NextDueOn { get; private set; }
    public string? Note { get; private set; }
    public IReadOnlyCollection<RecurringBillOccurrence> Occurrences => occurrences.AsReadOnly();

    public static ErrorOr<RecurringBill> Create(
        Guid householdId,
        string name,
        Account account,
        Category? category,
        Money amount,
        RecurringBillCadence cadence,
        RecurringBillType type,
        RecurringBillDirection direction,
        DateOnly startsOn,
        string? note)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            return EconomyErrors.RecurringBillInvalid;
        }

        if (account.HouseholdId != householdId)
        {
            return EconomyErrors.AccountNotFound;
        }

        if (category is not null && category.HouseholdId != householdId)
        {
            return EconomyErrors.CategoryNotFound;
        }

        return new RecurringBill(
            RecurringBillId.New(),
            householdId,
            name.Trim(),
            account.Id,
            category?.Id,
            amount,
            cadence,
            type,
            direction,
            startsOn,
            cadence.NextDueOn(startsOn),
            note);
    }

    public ErrorOr<RecurringBillOccurrence> MarkSkipped(DateOnly dueOn)
    {
        if (dueOn < NextDueOn || HasOccurrence(dueOn))
        {
            return EconomyErrors.RecurringBillOccurrenceInvalid;
        }

        var occurrence = RecurringBillOccurrence.Skipped(Id, dueOn);
        occurrences.Add(occurrence);
        AdvanceNextDueOn(dueOn);
        return occurrence;
    }

    public ErrorOr<RecurringBillOccurrence> MarkPaused(DateOnly dueOn)
    {
        if (dueOn < NextDueOn || HasOccurrence(dueOn))
        {
            return EconomyErrors.RecurringBillOccurrenceInvalid;
        }

        var occurrence = RecurringBillOccurrence.Paused(Id, dueOn);
        occurrences.Add(occurrence);
        AdvanceNextDueOn(dueOn);
        return occurrence;
    }

    public ErrorOr<Success> Resume(DateOnly dueOn)
    {
        var occurrence = occurrences.SingleOrDefault(x => x.DueOn == dueOn);
        if (occurrence is null || occurrence.State != RecurringBillOccurrenceState.Paused)
        {
            return EconomyErrors.RecurringBillOccurrenceInvalid;
        }

        occurrence.Resume();
        return Result.Success;
    }

    public ErrorOr<Transaction> PostDue(Account account, Category? category, DateOnly dueOn)
    {
        if (!CanRun(dueOn) || Type != RecurringBillType.Fixed)
        {
            return EconomyErrors.RecurringBillOccurrenceInvalid;
        }

        var transaction = Transaction.Record(
            HouseholdId,
            account,
            category,
            Amount,
            dueOn,
            Note ?? Name,
            Direction.ToTransactionKind(),
            payerId: null);
        if (transaction.IsError)
        {
            return transaction.Errors;
        }

        occurrences.Add(RecurringBillOccurrence.Posted(Id, dueOn, transaction.Value.Id));
        AdvanceNextDueOn(dueOn);
        return transaction.Value;
    }

    public ErrorOr<Transaction> CreatePending(Account account, Category? category, DateOnly dueOn)
    {
        if (!CanRun(dueOn) || Type != RecurringBillType.Estimated)
        {
            return EconomyErrors.RecurringBillOccurrenceInvalid;
        }

        var transaction = Transaction.RecordPending(
            HouseholdId,
            account,
            category,
            Amount,
            dueOn,
            Note ?? Name,
            Direction.ToTransactionKind(),
            payerId: null);
        if (transaction.IsError)
        {
            return transaction.Errors;
        }

        occurrences.Add(RecurringBillOccurrence.Pending(Id, dueOn, transaction.Value.Id));
        AdvanceNextDueOn(dueOn);
        return transaction.Value;
    }

    public ErrorOr<Success> ConfirmPending(Transaction transaction)
    {
        var occurrence = occurrences.SingleOrDefault(x =>
            x.TransactionId == transaction.Id &&
            x.State == RecurringBillOccurrenceState.Pending);
        if (occurrence is null)
        {
            return EconomyErrors.TransactionNotPending;
        }

        occurrence.Confirm(transaction.Id);
        return Result.Success;
    }

    public bool IsDue(DateOnly date) => NextDueOn <= date;

    private bool CanRun(DateOnly dueOn) => dueOn == NextDueOn && !HasOccurrence(dueOn);

    private bool HasOccurrence(DateOnly dueOn) =>
        occurrences.Any(x => x.DueOn == dueOn &&
                             x.State != RecurringBillOccurrenceState.Paused);

    private void AdvanceNextDueOn(DateOnly occurrence) =>
        NextDueOn = Cadence.NextAfter(occurrence);

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }
}
