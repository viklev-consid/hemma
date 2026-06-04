using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;

namespace Hemma.Modules.Households;

public sealed record HouseholdRef(HouseholdId Id, string Name, string Slug);

public interface IHouseholdRefResolver
{
    Task<ErrorOr<HouseholdRef>> ResolveAsync(string householdRef, CancellationToken ct);
}

internal sealed class HouseholdRefResolver(HouseholdsDbContext db) : IHouseholdRefResolver
{
    public async Task<ErrorOr<HouseholdRef>> ResolveAsync(string householdRef, CancellationToken ct)
    {
        Household? household;
        if (Guid.TryParse(householdRef, out var id))
        {
            var householdId = new HouseholdId(id);
            household = await db.Households
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == householdId, ct);
        }
        else
        {
            var slugResult = HouseholdSlug.Create(householdRef);
            if (slugResult.IsError)
            {
                return slugResult.Errors;
            }

            var slug = slugResult.Value;
            household = await db.Households
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Slug == slug, ct);
        }

        if (household is null || household.IsDeleted)
        {
            return HouseholdsErrors.HouseholdNotFound;
        }

        return new HouseholdRef(household.Id, household.Name, household.Slug.Value);
    }
}
