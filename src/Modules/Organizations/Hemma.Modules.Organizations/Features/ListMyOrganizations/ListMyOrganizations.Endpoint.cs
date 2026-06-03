using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Organizations.Features.ListMyOrganizations;

internal static class ListMyOrganizationsEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(OrganizationsRoutes.MyOrganizations,
            async (ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListMyOrganizationsResponse>>(
                    new ListMyOrganizationsQuery(userId),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ListMyOrganizations")
        .WithSummary("List organizations where the caller has an active membership.")
        .Produces<ListMyOrganizationsResponse>()
        .RequireAuthorization();
}
