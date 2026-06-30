using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Infrastructure.Blobs;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.DeleteTransaction;

public sealed class DeleteTransactionHandler(EconomyDbContext db, IBlobStore blobStore, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<Success>> Handle(DeleteTransactionCommand cmd, CancellationToken ct)
    {
        var transactionId = new TransactionId(cmd.TransactionId);
        var transaction = await db.Transactions
            .SingleOrDefaultAsync(x => x.HouseholdId == cmd.HouseholdId && x.Id == transactionId, ct);
        if (transaction is null)
        {
            return EconomyErrors.TransactionNotFound;
        }

        var canDelete = transaction.EnsureCanDelete();
        if (canDelete.IsError)
        {
            return canDelete.Errors;
        }

        var receiptBlob = transaction.ReceiptBlobContainer is not null && transaction.ReceiptBlobKey is not null
            ? new BlobRef(transaction.ReceiptBlobContainer, transaction.ReceiptBlobKey)
            : null;

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(ct);

        if (receiptBlob is not null)
        {
            await blobStore.DeleteAsync(receiptBlob, ct);
        }

        await audit.PublishAsync(cmd.HouseholdId, "economy.transaction.deleted", "Transaction", cmd.TransactionId, null, ct);
        return Result.Success;
    }
}
