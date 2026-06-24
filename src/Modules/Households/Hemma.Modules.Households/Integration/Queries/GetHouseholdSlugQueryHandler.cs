using Hemma.Modules.Households.Contracts.Queries;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Households.Integration.Queries;

/// <summary>
/// Handles the public <see cref="GetHouseholdSlugQuery"/> contract. Returns the current slug of a
/// non-deleted household so other modules can build household-scoped deep links. Returns
/// <c>null</c> when the household does not exist or has been soft-deleted.
/// </summary>
public sealed class GetHouseholdSlugQueryHandler(HouseholdsDbContext db)
{
    public async Task<GetHouseholdSlugResult?> Handle(GetHouseholdSlugQuery query, CancellationToken ct)
    {
        var householdId = new HouseholdId(query.HouseholdId);

        var slug = await db.Households
            .AsNoTracking()
            .Where(h => h.Id == householdId && !h.IsDeleted)
            .Select(h => h.Slug)
            .FirstOrDefaultAsync(ct);

        return slug is null ? null : new GetHouseholdSlugResult(slug.Value);
    }
}
