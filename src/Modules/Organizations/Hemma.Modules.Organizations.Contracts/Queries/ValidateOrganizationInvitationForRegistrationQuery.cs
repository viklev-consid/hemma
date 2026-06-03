using ErrorOr;

namespace Hemma.Modules.Organizations.Contracts.Queries;

public sealed record ValidateOrganizationInvitationForRegistrationQuery(string InvitationToken, string Email);

public sealed record ValidateOrganizationInvitationForRegistrationResponse(Guid OrganizationId, string Role);
