using Hemma.Shared.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.GetBudgetSummary;

internal static class GetBudgetSummaryEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{EconomyRoutes.Prefix}/budget-summary",
            async (
                Guid householdId,
                DateOnly anchorDate,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetBudgetSummaryResponse>>(new GetBudgetSummaryQuery(householdId, anchorDate), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetEconomyBudgetSummary")
        .WithSummary("Get economy budget planned vs actual summary.")
        .Produces<GetBudgetSummaryResponse>()
        .RequireAuthorization();
}
