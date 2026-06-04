using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Contracts.Commands;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;

namespace Hemma.Modules.Households.Features.EnsureUserCanBeErasedFromHouseholds;

public sealed class EnsureUserCanBeErasedFromHouseholdsHandler(HouseholdsDbContext db)
{
    public async Task<ErrorOr<EnsureUserCanBeErasedFromHouseholdsResponse>> Handle(EnsureUserCanBeErasedFromHouseholdsCommand cmd, CancellationToken ct)
    {
        var ownedHouseholds = await db.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == cmd.UserId && m.IsActive && m.Role == HouseholdRole.Owner)
            .Join(
                db.Households.AsNoTracking().Where(o => !o.IsDeleted),
                membership => membership.HouseholdId,
                household => household.Id,
                (membership, household) => new
                {
                    household.Id,
                    household.Name,
                    household.Slug,
                    membership.Role
                })
            .OrderBy(o => o.Name)
            .ToArrayAsync(ct);

        if (ownedHouseholds.Length == 0)
        {
            return new EnsureUserCanBeErasedFromHouseholdsResponse([]);
        }

        var householdIds = ownedHouseholds.Select(o => o.Id).ToArray();
        var ownerCounts = await db.Memberships
            .AsNoTracking()
            .Where(m => householdIds.Contains(m.HouseholdId) && m.IsActive && m.Role == HouseholdRole.Owner)
            .GroupBy(m => m.HouseholdId)
            .Select(g => new { HouseholdId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HouseholdId, x => x.Count, ct);

        var blockers = ownedHouseholds
            .Select(o => new UserErasureBlockingHousehold(
                o.Id.Value,
                o.Name,
                o.Slug.Value,
                o.Role.Name,
                ownerCounts.TryGetValue(o.Id, out var count) && count == 1))
            .ToArray();

        return new EnsureUserCanBeErasedFromHouseholdsResponse(blockers);
    }
}
