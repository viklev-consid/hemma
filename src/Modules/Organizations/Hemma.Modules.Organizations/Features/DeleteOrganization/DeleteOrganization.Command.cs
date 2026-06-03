using Hemma.Modules.Organizations.Domain;

namespace Hemma.Modules.Organizations.Features.DeleteOrganization;

public sealed record DeleteOrganizationCommand(OrganizationId OrganizationId, Guid DeletedByUserId);
