using ErrorOr;
using Hemma.Modules.Economy.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Economy.Domain;

public sealed class Transaction : AggregateRoot<TransactionId>
{
    private Transaction(
        TransactionId id,
        Guid householdId,
        AccountId accountId,
        CategoryId? categoryId,
        Money amount,
        DateOnly occurredOn,
        string? note,
        TransactionKind kind,
        Guid? payerId,
        TransferId? transferId,
        bool isTransferOutflow,
        bool isPending,
        string? importFingerprint = null) : base(id)
    {
        HouseholdId = householdId;
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        OccurredOn = occurredOn;
        Note = note;
        Kind = kind;
        PayerId = payerId;
        TransferId = transferId;
        IsTransferOutflow = isTransferOutflow;
        IsPending = isPending;
        ImportFingerprint = importFingerprint;
    }

    private Transaction() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public AccountId AccountId { get; private set; } = null!;
    public CategoryId? CategoryId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateOnly OccurredOn { get; private set; }
    public string? Note { get; private set; }
    public TransactionKind Kind { get; private set; } = null!;
    public string? ReceiptBlobContainer { get; private set; }
    public string? ReceiptBlobKey { get; private set; }
    public Guid? SubscriptionId { get; private set; }
    public Guid? PayerId { get; private set; }
    public TransferId? TransferId { get; private set; }
    public bool IsTransferOutflow { get; private set; }
    public bool IsPending { get; private set; }
    public string? ImportFingerprint { get; private set; }

    public bool HasReceipt => ReceiptBlobContainer is not null && ReceiptBlobKey is not null;

    public static ErrorOr<Transaction> Record(
        Guid householdId,
        Account account,
        Category? category,
        Money amount,
        DateOnly occurredOn,
        string? note,
        TransactionKind kind,
        Guid? payerId)
    {
        if (account.HouseholdId != householdId)
        {
            return EconomyErrors.AccountNotFound;
        }

        if (kind == TransactionKind.Transfer)
        {
            return EconomyErrors.TransactionKindInvalid;
        }

        if (category is not null && category.HouseholdId != householdId)
        {
            return EconomyErrors.CategoryNotFound;
        }

        return new Transaction(
            TransactionId.New(),
            householdId,
            account.Id,
            category?.Id,
            amount,
            occurredOn,
            NormalizeNote(note),
            kind,
            payerId,
            transferId: null,
            isTransferOutflow: false,
            isPending: false);
    }

    public static ErrorOr<Transaction> RecordImported(
        Guid householdId,
        Account account,
        Category? category,
        Money amount,
        DateOnly occurredOn,
        string description,
        TransactionKind kind,
        string importFingerprint)
    {
        if (string.IsNullOrWhiteSpace(importFingerprint) || importFingerprint.Length > 128)
        {
            return EconomyErrors.ImportFingerprintInvalid;
        }

        return new Transaction(
            TransactionId.New(),
            householdId,
            account.Id,
            category?.Id,
            amount,
            occurredOn,
            NormalizeNote(description),
            kind,
            payerId: null,
            transferId: null,
            isTransferOutflow: false,
            isPending: false,
            importFingerprint.Trim());
    }

    public static ErrorOr<Transaction> RecordPending(
        Guid householdId,
        Account account,
        Category? category,
        Money amount,
        DateOnly occurredOn,
        string? note,
        TransactionKind kind,
        Guid? payerId)
    {
        var transaction = Record(householdId, account, category, amount, occurredOn, note, kind, payerId);
        if (transaction.IsError)
        {
            return transaction.Errors;
        }

        transaction.Value.IsPending = true;
        return transaction;
    }

    public static ErrorOr<Transaction> CreateTransferLeg(
        Guid householdId,
        Account account,
        Category? category,
        Money amount,
        DateOnly occurredOn,
        string? note,
        Guid? payerId,
        TransferId transferId,
        bool isOutflow)
    {
        if (account.HouseholdId != householdId)
        {
            return EconomyErrors.AccountNotFound;
        }

        if (category is not null && category.HouseholdId != householdId)
        {
            return EconomyErrors.CategoryNotFound;
        }

        return new Transaction(
            TransactionId.New(),
            householdId,
            account.Id,
            category?.Id,
            amount,
            occurredOn,
            NormalizeNote(note),
            TransactionKind.Transfer,
            payerId,
            transferId,
            isOutflow,
            isPending: false);
    }

    public ErrorOr<Success> AttachReceipt(string container, string key)
    {
        if (string.IsNullOrWhiteSpace(container) || string.IsNullOrWhiteSpace(key))
        {
            return EconomyErrors.ReceiptBlobInvalid;
        }

        ReceiptBlobContainer = container.Trim();
        ReceiptBlobKey = key.Trim();
        return Result.Success;
    }

    public ErrorOr<Success> ConfirmPending(Money amount, DateOnly occurredOn)
    {
        if (!IsPending)
        {
            return EconomyErrors.TransactionNotPending;
        }

        if (!string.Equals(Amount.Currency, amount.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return EconomyErrors.CurrencyMismatch;
        }

        Amount = amount;
        OccurredOn = occurredOn;
        IsPending = false;
        return Result.Success;
    }

    public ErrorOr<Success> LinkToSubscription(Guid subscriptionId)
    {
        if (subscriptionId == Guid.Empty)
        {
            return EconomyErrors.SubscriptionNotFound;
        }

        SubscriptionId = subscriptionId;
        return Result.Success;
    }

    public ErrorOr<Success> UnlinkSubscription(Guid subscriptionId)
    {
        if (SubscriptionId != subscriptionId)
        {
            return EconomyErrors.TransactionSubscriptionLinkNotFound;
        }

        SubscriptionId = null;
        return Result.Success;
    }

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
