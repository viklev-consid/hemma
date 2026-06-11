using ErrorOr;

namespace Hemma.Modules.Households.Contracts.Queries;

public sealed record ValidateHouseholdInvitationForRegistrationQuery(string InvitationToken, string Email);

public sealed record ValidateHouseholdInvitationForRegistrationResponse(Guid HouseholdId, string Role);
