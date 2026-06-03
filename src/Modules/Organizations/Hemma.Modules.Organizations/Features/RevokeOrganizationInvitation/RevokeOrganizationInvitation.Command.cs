using Hemma.Modules.Organizations.Domain;

namespace Hemma.Modules.Organizations.Features.RevokeOrganizationInvitation;

public sealed record RevokeOrganizationInvitationCommand(OrganizationId OrganizationId, OrganizationInvitationId InvitationId, Guid RevokedByUserId);
