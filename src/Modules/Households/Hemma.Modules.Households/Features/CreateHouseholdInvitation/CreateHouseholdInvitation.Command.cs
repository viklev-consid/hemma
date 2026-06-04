using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Features.CreateHouseholdInvitation;

public sealed record CreateHouseholdInvitationCommand(HouseholdId HouseholdId, string Email, string Role, Guid InvitedByUserId);
