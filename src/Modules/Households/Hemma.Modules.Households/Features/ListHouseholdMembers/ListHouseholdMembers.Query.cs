using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.ListHouseholdMembers;

public sealed record ListHouseholdMembersQuery(HouseholdId HouseholdId, int Page = 1, int PageSize = 20);
