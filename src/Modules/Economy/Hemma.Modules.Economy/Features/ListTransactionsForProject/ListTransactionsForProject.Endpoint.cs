using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.ListTransactionsForProject;

internal static class ListTransactionsForProjectEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{EconomyRoutes.Prefix}/projects/{{projectId:guid}}/transactions",
            async (
                Guid projectId,
                Guid householdId,
                int? page,
                int? pageSize,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ListTransactionsForProjectResponse>(
                    new ListTransactionsForProjectQuery(householdId, projectId, page ?? 1, pageSize ?? 50),
                    ct);
                return Results.Ok(result);
            })
        .WithName("ListEconomyTransactionsForProject")
        .WithSummary("List economy transactions linked to a property project.")
        .Produces<ListTransactionsForProjectResponse>()
        .RequireAuthorization();
}
