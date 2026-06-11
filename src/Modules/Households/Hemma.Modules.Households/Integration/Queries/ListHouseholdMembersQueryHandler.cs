using Hemma.Modules.Households.Contracts.Queries;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Households.Integration.Queries;

/// <summary>
/// Handles the public <see cref="ListHouseholdMembersQuery"/> contract. Returns the active,
/// non-anonymised members of a household so other modules can fan out to every member.
/// </summary>
public sealed class ListHouseholdMembersQueryHandler(HouseholdsDbContext db)
{
    public async Task<ListHouseholdMembersResult> Handle(ListHouseholdMembersQuery query, CancellationToken ct)
    {
        var householdId = new HouseholdId(query.HouseholdId);

        var members = await db.Memberships
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId && m.IsActive && !m.IsAnonymized && m.UserId != null)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new HouseholdMemberInfo(m.UserId!.Value, m.Role.Name))
            .ToArrayAsync(ct);

        return new ListHouseholdMembersResult(members);
    }
}
