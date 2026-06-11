using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.ChangeHouseholdMemberRole;

public sealed record ChangeHouseholdMemberRoleCommand(HouseholdId HouseholdId, Guid UserId, string Role, Guid ChangedByUserId);
