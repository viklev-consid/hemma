using Hemma.Modules.Organizations.Domain;

namespace Hemma.Modules.Organizations.Features.ChangeOrganizationMemberRole;

public sealed record ChangeOrganizationMemberRoleCommand(OrganizationId OrganizationId, Guid UserId, string Role, Guid ChangedByUserId);
