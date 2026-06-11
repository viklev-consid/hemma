using ErrorOr;
using Hemma.Modules.Households.Authorization;
using Hemma.Modules.Households.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Households.Features.ListMyHouseholds;

public sealed class ListMyHouseholdsHandler(HouseholdsDbContext db)
{
    public async Task<ErrorOr<ListMyHouseholdsResponse>> Handle(ListMyHouseholdsQuery query, CancellationToken ct)
    {
        var rows = await db.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == query.UserId && m.IsActive)
            .Join(
                db.Households.AsNoTracking().Where(o => !o.IsDeleted),
                membership => membership.HouseholdId,
                household => household.Id,
                (membership, household) => new
                {
                    HouseholdId = household.Id,
                    household.Name,
                    household.Slug,
                    membership.Role,
                })
            .OrderBy(o => o.Name)
            .ToArrayAsync(ct);

        var households = rows
            .Select(row => new MyHouseholdItem(
                row.HouseholdId.Value,
                row.Name,
                row.Slug.Value,
                row.Role.Name,
                HouseholdRolePermissionMap.GetPermissions(row.Role),
                HouseholdRolePermissionMap.GetVersion(row.Role)))
            .ToArray();

        return new ListMyHouseholdsResponse(households);
    }
}
