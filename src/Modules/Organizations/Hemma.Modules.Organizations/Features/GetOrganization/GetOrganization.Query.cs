using Hemma.Modules.Organizations.Domain;
using Hemma.Shared.Infrastructure.Authorization;

namespace Hemma.Modules.Organizations.Features.GetOrganization;

public sealed record GetOrganizationQuery(OrganizationId OrganizationId, ScopedAuthorizationAccessMode AccessMode);
