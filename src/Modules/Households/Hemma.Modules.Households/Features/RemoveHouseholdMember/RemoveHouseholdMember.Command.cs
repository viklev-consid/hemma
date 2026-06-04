using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.RemoveHouseholdMember;

public sealed record RemoveHouseholdMemberCommand(HouseholdId HouseholdId, Guid UserId, Guid RemovedByUserId);
