using Hemma.Modules.Organizations.Domain;

namespace Hemma.Modules.Organizations.Features.ListOrganizationInvitations;

public sealed record ListOrganizationInvitationsQuery(OrganizationId OrganizationId, int Page = 1, int PageSize = 20);
