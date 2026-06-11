using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Households.Errors;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Pagination;

namespace Hemma.Modules.Households.Features.ListHouseholdInvitations;

public sealed class ListHouseholdInvitationsHandler(HouseholdsDbContext db)
{
    public async Task<ErrorOr<ListHouseholdInvitationsResponse>> Handle(ListHouseholdInvitationsQuery query, CancellationToken ct)
    {
        if (query.PageSize <= 0 || query.PageSize > PageRequest.MaxPageSize)
        {
            return HouseholdsErrors.PageSizeInvalid;
        }

        var pagination = PageRequest.Of(query.Page, query.PageSize);
        var baseQuery = db.Invitations
            .AsNoTracking()
            .Where(i => i.HouseholdId == query.HouseholdId);
        var total = await baseQuery.CountAsync(ct);
        var invitations = await baseQuery
            .OrderByDescending(i => i.InvitedAt)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(i => new HouseholdInvitationItem(i.Id.Value, i.Email, i.Role.Name, i.ExpiresAt, i.IsPending))
            .ToArrayAsync(ct);

        return new ListHouseholdInvitationsResponse(invitations, pagination.Page, pagination.PageSize, total);
    }
}
