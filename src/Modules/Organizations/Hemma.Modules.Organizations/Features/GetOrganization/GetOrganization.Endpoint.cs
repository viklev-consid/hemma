using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Modules.Organizations.Authorization;
using Hemma.Modules.Organizations.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Organizations.Features.GetOrganization;

internal static class GetOrganizationEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(OrganizationsRoutes.ByRef,
            async (
                string organizationRef,
                IOrganizationRefResolver resolver,
                IScopedAuthorizationService<OrganizationScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var organization = await resolver.ResolveAsync(organizationRef, ct);
                if (organization.IsError)
                {
                    return organization.ToProblemDetailsOr(_ => Results.NotFound());
                }

                var access = await authorization.AuthorizeAsync(
                    currentUser,
                    organization.Value,
                    OrganizationsPermissions.OrganizationsRead,
                    ScopedAuthorizationOptions.WithPlatformOverride,
                    ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetOrganizationResponse>>(
                    new GetOrganizationQuery(organization.Value.Id, access.AccessMode),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetOrganization")
        .WithSummary("Get an organization by ID or slug.")
        .Produces<GetOrganizationResponse>()
        .RequireAuthorization();
}
