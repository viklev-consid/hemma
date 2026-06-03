using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Modules.Audit.Contracts.Queries;
using Hemma.Modules.Organizations.Authorization;
using Hemma.Modules.Organizations.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Organizations.Features.GetOrganizationAudit;

internal static class GetOrganizationAuditEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(OrganizationsRoutes.Audit,
            async (
                string organizationRef,
                IOrganizationRefResolver resolver,
                IScopedAuthorizationService<OrganizationScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                int? page,
                int? pageSize,
                CancellationToken ct) =>
            {
                var organization = await resolver.ResolveAsync(organizationRef, ct);
                if (organization.IsError)
                {
                    return organization.ToProblemDetailsOr(_ => Results.Empty);
                }

                var access = await authorization.AuthorizeAsync(
                    currentUser,
                    organization.Value,
                    OrganizationsPermissions.AuditRead,
                    ScopedAuthorizationOptions.WithPlatformOverride,
                    ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }

                var audit = await bus.InvokeAsync<ErrorOr.ErrorOr<ListOrganizationAuditEntriesResponse>>(
                    new ListOrganizationAuditEntriesQuery(organization.Value.Id.Value, page ?? 1, pageSize ?? 20),
                    ct);

                return audit.ToProblemDetailsOr(response => Results.Ok(new GetOrganizationAuditResponse(
                        organization.Value.Id.Value,
                        access.AccessMode.ToString(),
                        response.Entries,
                        response.Total,
                        response.Page,
                        response.PageSize)));
            })
        .WithName("GetOrganizationAudit")
        .WithSummary("Get organization audit entries.")
        .Produces<GetOrganizationAuditResponse>()
        .RequireAuthorization();
}
