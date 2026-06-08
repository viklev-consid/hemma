using Hemma.Modules.Economy.Persistence;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Gdpr;

public sealed class EconomyPersonalDataEraser(EconomyDbContext db, IBlobStore blobStore) : IPersonalDataEraser
{
    public async Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        var recordsAffected = await EraseUserReferencesAsync(user.UserId, householdId: null, ct);
        return new ErasureResult(user.UserId, strategy, recordsAffected);
    }

    public Task<int> EraseHouseholdMemberAsync(Guid householdId, Guid userId, CancellationToken ct) =>
        EraseUserReferencesAsync(userId, householdId, ct);

    private async Task<int> EraseUserReferencesAsync(Guid userId, Guid? householdId, CancellationToken ct)
    {
        var transactions = await db.Transactions
            .Where(transaction => householdId.HasValue
                ? transaction.HouseholdId == householdId.Value
                : transaction.PayerId == userId)
            .ToListAsync(ct);

        foreach (var transaction in transactions)
        {
            if (transaction.ReceiptBlobContainer is not null && transaction.ReceiptBlobKey is not null)
            {
                await blobStore.DeleteAsync(
                    new BlobRef(transaction.ReceiptBlobContainer, transaction.ReceiptBlobKey),
                    ct);
                transaction.ClearReceipt();
            }

            transaction.AnonymizePersonalData(userId);
        }

        await db.SaveChangesAsync(ct);
        return transactions.Count;
    }
}
