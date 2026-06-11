using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.ListTransactions;

internal static class ListTransactionsEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet($"{EconomyRoutes.Prefix}/transactions",
            async (
                Guid householdId,
                Guid? categoryId,
                DateOnly? from,
                DateOnly? to,
                Guid? payerId,
                bool? hasReceipt,
                decimal? minAmount,
                decimal? maxAmount,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListTransactionsResponse>>(
                    new ListTransactionsQuery(householdId, categoryId, from, to, payerId, hasReceipt, minAmount, maxAmount, page ?? 1, pageSize ?? 50),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ListEconomyTransactions")
        .WithSummary("List economy transactions with filters.")
        .Produces<ListTransactionsResponse>()
        .RequireAuthorization();
}
