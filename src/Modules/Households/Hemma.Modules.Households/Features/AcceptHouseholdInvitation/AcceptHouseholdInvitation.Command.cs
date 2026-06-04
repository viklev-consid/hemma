namespace Hemma.Modules.Households.Features.AcceptHouseholdInvitation;

public sealed record AcceptHouseholdInvitationCommand(string InvitationToken, Guid UserId, string Email);
