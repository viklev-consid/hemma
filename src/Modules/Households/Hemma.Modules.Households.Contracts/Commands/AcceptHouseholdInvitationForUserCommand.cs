using ErrorOr;

namespace Hemma.Modules.Households.Contracts.Commands;

public sealed record AcceptHouseholdInvitationForUserCommand(
    string InvitationToken,
    Guid UserId,
    string Email);

public sealed record AcceptedHouseholdInvitationForUserResponse(
    Guid HouseholdId,
    string Role);
