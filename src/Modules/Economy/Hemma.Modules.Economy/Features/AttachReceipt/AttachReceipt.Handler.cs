using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Infrastructure.Blobs;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.AttachReceipt;

public sealed class AttachReceiptHandler(EconomyDbContext db, IBlobStore blobStore)
{
    private static readonly HashSet<string> allowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    public async Task<ErrorOr<AttachReceiptResponse>> Handle(AttachReceiptCommand cmd, CancellationToken ct)
    {
        if (cmd.Content.Length == 0 ||
            cmd.Content.Length > 10 * 1024 * 1024 ||
            !allowedContentTypes.Contains(cmd.ContentType))
        {
            return EconomyErrors.ReceiptFileInvalid;
        }

        var transactionId = new TransactionId(cmd.TransactionId);
        var transaction = await db.Transactions
            .SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == transactionId, ct);
        if (transaction is null)
        {
            return EconomyErrors.TransactionNotFound;
        }

        await using var stream = new MemoryStream(cmd.Content, writable: false);
        var blobRef = await blobStore.PutAsync(
            stream,
            new BlobMetadata(cmd.ContentType, cmd.Content.LongLength, cmd.FileName),
            ct);

        var previousContainer = transaction.ReceiptBlobContainer;
        var previousKey = transaction.ReceiptBlobKey;
        var attached = transaction.AttachReceipt(blobRef.Container, blobRef.Key);
        if (attached.IsError)
        {
            await blobStore.DeleteAsync(blobRef, ct);
            return attached.Errors;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await blobStore.DeleteAsync(blobRef, ct);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousContainer) && !string.IsNullOrWhiteSpace(previousKey))
        {
            await blobStore.DeleteAsync(new BlobRef(previousContainer, previousKey), ct);
        }

        return new AttachReceiptResponse(transaction.Id.Value, blobRef.Container, blobRef.Key);
    }
}
