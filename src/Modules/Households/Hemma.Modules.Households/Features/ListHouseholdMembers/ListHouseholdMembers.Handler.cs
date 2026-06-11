using ErrorOr;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Users.Contracts.Queries;
using Hemma.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Households.Features.ListHouseholdMembers;

public sealed class ListHouseholdMembersHandler(HouseholdsDbContext db, IMessageBus bus)
{
    public async Task<ErrorOr<ListHouseholdMembersResponse>> Handle(ListHouseholdMembersQuery query, CancellationToken ct)
    {
        if (query.PageSize <= 0 || query.PageSize > PageRequest.MaxPageSize)
        {
            return HouseholdsErrors.PageSizeInvalid;
        }

        var pagination = PageRequest.Of(query.Page, query.PageSize);
        var baseQuery = db.Memberships
            .AsNoTracking()
            .Where(m => m.HouseholdId == query.HouseholdId && m.IsActive);
        var total = await baseQuery.CountAsync(ct);
        var memberships = await db.Memberships
            .AsNoTracking()
            .Where(m => m.HouseholdId == query.HouseholdId && m.IsActive)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .ToArrayAsync(ct);

        var userIdsToHydrate = memberships
            .Where(m => !m.IsAnonymized && m.UserId is not null)
            .Select(m => m.UserId!.Value)
            .Distinct()
            .ToArray();

        var summariesByUserId = new Dictionary<Guid, UserSummary>();
        if (userIdsToHydrate.Length > 0)
        {
            var summaries = await bus.InvokeAsync<ErrorOr<GetUserSummariesByIdsResponse>>(
                new GetUserSummariesByIdsQuery(userIdsToHydrate),
                ct);

            if (summaries.IsError)
            {
                return summaries.Errors;
            }

            foreach (var summary in summaries.Value.Users)
            {
                summariesByUserId[summary.UserId] = summary;
            }
        }

        var members = memberships
            .OrderBy(m => m.Role.Name, StringComparer.Ordinal)
            .ThenBy(m => m.JoinedAt)
            .Select(m =>
            {
                if (m.IsAnonymized || m.UserId is null || !summariesByUserId.TryGetValue(m.UserId.Value, out var summary))
                {
                    return new HouseholdMemberItem(m.UserId, m.Role.Name, m.JoinedAt, m.IsAnonymized, null, null);
                }

                return new HouseholdMemberItem(m.UserId, m.Role.Name, m.JoinedAt, m.IsAnonymized, summary.DisplayName, summary.Email);
            })
            .ToArray();

        return new ListHouseholdMembersResponse(members, pagination.Page, pagination.PageSize, total);
    }
}
