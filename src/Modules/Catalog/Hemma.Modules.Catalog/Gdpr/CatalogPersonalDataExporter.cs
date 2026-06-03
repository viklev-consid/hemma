using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Catalog.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Catalog.Gdpr;

public sealed class CatalogPersonalDataExporter(CatalogDbContext db) : IPersonalDataExporter
{
    public async Task<PersonalDataExport> ExportAsync(UserRef user, CancellationToken ct)
    {
        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.UserId == user.UserId, ct);

        if (customer is null)
        {
            return new PersonalDataExport(user.UserId, "Catalog", new Dictionary<string, object?>(StringComparer.Ordinal));
        }

        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["email"] = customer.Email,
            ["displayName"] = customer.DisplayName,
            ["createdAt"] = customer.CreatedAt,
        };

        return new PersonalDataExport(user.UserId, "Catalog", data);
    }
}
