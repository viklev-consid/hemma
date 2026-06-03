using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Catalog.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Catalog.Gdpr;

public sealed class CatalogPersonalDataEraser(CatalogDbContext db) : IPersonalDataEraser
{
    public async Task<ErasureResult> EraseAsync(UserRef user, ErasureStrategy strategy, CancellationToken ct)
    {
        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.UserId == user.UserId, ct);

        if (customer is null)
        {
            return new ErasureResult(user.UserId, ErasureStrategy.Anonymize, 0);
        }

        customer.Anonymize();
        await db.SaveChangesAsync(ct);

        return new ErasureResult(user.UserId, ErasureStrategy.Anonymize, RecordsAffected: 1);
    }
}
