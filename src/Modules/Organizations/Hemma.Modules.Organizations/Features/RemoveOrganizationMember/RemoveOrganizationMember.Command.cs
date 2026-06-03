using Hemma.Modules.Organizations.Domain;

namespace Hemma.Modules.Organizations.Features.RemoveOrganizationMember;

public sealed record RemoveOrganizationMemberCommand(OrganizationId OrganizationId, Guid UserId, Guid RemovedByUserId);
