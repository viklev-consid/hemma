using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Modules.Organizations.Authorization;
using Hemma.Modules.Organizations.Contracts.Authorization;
using Hemma.Modules.Organizations.Domain;
using Hemma.Modules.Organizations.Errors;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Organizations.Features.RevokeOrganizationInvitation;

internal static class RevokeOrganizationInvitationEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(OrganizationsRoutes.InvitationById,
            async (
                string organizationRef,
                Guid invitationId,
                IOrganizationRefResolver resolver,
                IScopedAuthorizationService<OrganizationScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var revokedByUserId))
                {
                    return Results.Unauthorized();
                }

                var organization = await resolver.ResolveAsync(organizationRef, ct);
                if (organization.IsError)
                {
                    return organization.ToProblemDetailsOr(_ => Results.Empty);
                }

                var access = await authorization.AuthorizeAsync(currentUser, organization.Value, OrganizationsPermissions.InvitationsManage, ScopedAuthorizationOptions.WithPlatformOverride, ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }
                if (access.AccessMode == ScopedAuthorizationAccessMode.PlatformOverride)
                {
                    return Results.Problem(title: "Forbidden", detail: OrganizationsErrors.PlatformOverrideMutationForbidden.Description, statusCode: StatusCodes.Status403Forbidden);
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Success>>(
                    new RevokeOrganizationInvitationCommand(organization.Value.Id, new OrganizationInvitationId(invitationId), revokedByUserId),
                    ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
        .WithName("RevokeOrganizationInvitation")
        .WithSummary("Revoke an organization invitation.")
        .Produces(StatusCodes.Status204NoContent)
        .RequireAuthorization();
}
