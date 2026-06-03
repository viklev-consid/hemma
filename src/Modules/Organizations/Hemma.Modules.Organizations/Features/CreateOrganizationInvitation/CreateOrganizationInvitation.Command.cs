using Hemma.Modules.Organizations.Domain;

namespace Hemma.Modules.Organizations.Features.CreateOrganizationInvitation;

public sealed record CreateOrganizationInvitationCommand(OrganizationId OrganizationId, string Email, string Role, Guid InvitedByUserId);
