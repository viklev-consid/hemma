using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.ListHouseholdInvitations;

public sealed record ListHouseholdInvitationsQuery(HouseholdId HouseholdId, int Page = 1, int PageSize = 20);
