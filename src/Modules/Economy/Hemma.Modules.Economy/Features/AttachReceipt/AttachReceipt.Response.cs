namespace Hemma.Modules.Economy.Features.AttachReceipt;

public sealed record AttachReceiptResponse(Guid TransactionId, string BlobContainer, string BlobKey);
