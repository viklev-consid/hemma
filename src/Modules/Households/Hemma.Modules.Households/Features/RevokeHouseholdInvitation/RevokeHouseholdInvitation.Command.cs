using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.RevokeHouseholdInvitation;

public sealed record RevokeHouseholdInvitationCommand(HouseholdId HouseholdId, HouseholdInvitationId InvitationId, Guid RevokedByUserId);
