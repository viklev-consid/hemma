using Hemma.Modules.Organizations.Domain;

namespace Hemma.Modules.Organizations.Features.UpdateOrganization;

public sealed record UpdateOrganizationCommand(OrganizationId OrganizationId, string Name, string Slug);
