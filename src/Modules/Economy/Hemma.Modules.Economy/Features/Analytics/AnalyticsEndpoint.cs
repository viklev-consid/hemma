using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.Analytics;

internal static class AnalyticsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet($"{EconomyRoutes.Prefix}/analytics/category-trend",
            async (
                Guid householdId,
                DateOnly from,
                DateOnly to,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetCategoryTrendResponse>>(
                    new GetCategoryTrendQuery(householdId, from, to),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetEconomyCategoryTrend")
            .WithSummary("Get category spend trends grouped by month.")
            .Produces<GetCategoryTrendResponse>()
            .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/analytics/spend-breakdown",
            async (
                Guid householdId,
                DateOnly from,
                DateOnly to,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetSpendBreakdownResponse>>(
                    new GetSpendBreakdownQuery(householdId, from, to),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetEconomySpendBreakdown")
            .WithSummary("Get category spend shares including savings allocations.")
            .Produces<GetSpendBreakdownResponse>()
            .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/analytics/period-comparison",
            async (
                Guid householdId,
                DateOnly anchorDate,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetPeriodComparisonResponse>>(
                    new GetPeriodComparisonQuery(householdId, anchorDate),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetEconomyPeriodComparison")
            .WithSummary("Compare current and previous budget periods.")
            .Produces<GetPeriodComparisonResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/analytics/income-vs-expense",
            async (
                Guid householdId,
                DateOnly from,
                DateOnly to,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetIncomeVsExpenseResponse>>(
                    new GetIncomeVsExpenseQuery(householdId, from, to),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetEconomyIncomeVsExpense")
            .WithSummary("Get income, expense, and net series grouped by month.")
            .Produces<GetIncomeVsExpenseResponse>()
            .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/analytics/variance-history",
            async (
                Guid householdId,
                DateOnly from,
                DateOnly to,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetVarianceHistoryResponse>>(
                    new GetVarianceHistoryQuery(householdId, from, to),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetEconomyVarianceHistory")
            .WithSummary("Get budget planned, actual, and variance history.")
            .Produces<GetVarianceHistoryResponse>()
            .RequireAuthorization();

        app.MapGet($"{EconomyRoutes.Prefix}/analytics/top-transactions",
            async (
                Guid householdId,
                DateOnly from,
                DateOnly to,
                Guid? categoryId,
                int? limit,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await AuthorizeAsync(householdId, authorization, currentUser, ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetTopTransactionsResponse>>(
                    new GetTopTransactionsQuery(
                        householdId,
                        from,
                        to,
                        categoryId is null ? null : new CategoryId(categoryId.Value),
                        limit ?? 10),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("GetEconomyTopTransactions")
            .WithSummary("Get largest posted non-transfer transactions.")
            .Produces<GetTopTransactionsResponse>()
            .RequireAuthorization();
    }

    private static Task<IResult?> AuthorizeAsync(
        Guid householdId,
        IScopedAuthorizationService<HouseholdScope> authorization,
        ICurrentUser currentUser,
        CancellationToken ct) =>
        EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
            householdId,
            HouseholdsPermissions.HouseholdsRead,
            authorization,
            currentUser,
            ct);
}
