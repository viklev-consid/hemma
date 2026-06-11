using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Households.Features.ListMyHouseholds;

internal static class ListMyHouseholdsEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(HouseholdsRoutes.MyHouseholds,
            async (ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListMyHouseholdsResponse>>(
                    new ListMyHouseholdsQuery(userId),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ListMyHouseholds")
        .WithSummary("List households where the caller has an active membership.")
        .Produces<ListMyHouseholdsResponse>()
        .RequireAuthorization();
}
