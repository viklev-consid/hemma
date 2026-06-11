namespace Hemma.Modules.Economy.Features.AttachReceipt;

public sealed record AttachReceiptCommand(
    Guid HouseholdId,
    Guid TransactionId,
    byte[] Content,
    string ContentType,
    string? FileName);
