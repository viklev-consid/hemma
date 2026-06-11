using Hemma.Shared.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.ListAccounts;

internal static class ListAccountsEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{EconomyRoutes.Prefix}/accounts",
            async (
                Guid householdId,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListAccountsResponse>>(new ListAccountsQuery(householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ListEconomyAccounts")
        .WithSummary("List economy accounts for a household.")
        .Produces<ListAccountsResponse>()
        .RequireAuthorization();
}
