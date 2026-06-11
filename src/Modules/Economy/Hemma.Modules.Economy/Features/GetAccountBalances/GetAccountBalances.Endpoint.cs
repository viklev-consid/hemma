using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Contracts;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.GetAccountBalances;

internal static class GetAccountBalancesEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{EconomyRoutes.Prefix}/accounts/balances",
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetAccountBalancesResponse>>(new GetAccountBalancesQuery(householdId), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetEconomyAccountBalances")
        .WithSummary("Get economy account balances.")
        .Produces<GetAccountBalancesResponse>()
        .RequireAuthorization();
}
