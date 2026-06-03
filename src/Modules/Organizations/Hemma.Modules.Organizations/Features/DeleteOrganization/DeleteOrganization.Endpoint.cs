using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Modules.Organizations.Authorization;
using Hemma.Modules.Organizations.Contracts.Authorization;
using Hemma.Modules.Organizations.Errors;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Organizations.Features.DeleteOrganization;

internal static class DeleteOrganizationEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(OrganizationsRoutes.ByRef,
            async (
                string organizationRef,
                IOrganizationRefResolver resolver,
                IScopedAuthorizationService<OrganizationScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var organization = await resolver.ResolveAsync(organizationRef, ct);
                if (organization.IsError)
                {
                    return organization.ToProblemDetailsOr(_ => Results.Empty);
                }

                var access = await authorization.AuthorizeAsync(
                    currentUser,
                    organization.Value,
                    OrganizationsPermissions.OrganizationsDelete,
                    ScopedAuthorizationOptions.WithPlatformOverride,
                    ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }
                if (access.AccessMode == ScopedAuthorizationAccessMode.PlatformOverride)
                {
                    return Results.Problem(title: "Forbidden", detail: OrganizationsErrors.PlatformOverrideMutationForbidden.Description, statusCode: StatusCodes.Status403Forbidden);
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Success>>(
                    new DeleteOrganizationCommand(organization.Value.Id, userId),
                    ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
        .WithName("DeleteOrganization")
        .WithSummary("Soft-delete an organization.")
        .Produces(StatusCodes.Status204NoContent)
        .RequireAuthorization();
}
