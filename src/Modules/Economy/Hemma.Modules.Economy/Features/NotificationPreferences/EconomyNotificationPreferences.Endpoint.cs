using ErrorOr;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.NotificationPreferences;

internal static class EconomyNotificationPreferencesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet($"{EconomyRoutes.Prefix}/notification-preferences",
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

                var response = await bus.InvokeAsync<EconomyNotificationPreferencesResponse>(
                    new GetEconomyNotificationPreferencesQuery(householdId),
                    ct);
                return Results.Ok(response);
            })
        .WithName("GetEconomyNotificationPreferences")
        .WithSummary("Get economy alert preferences for a household.")
        .Produces<EconomyNotificationPreferencesResponse>()
        .RequireAuthorization();

        app.MapPut($"{EconomyRoutes.Prefix}/notification-preferences",
            async (
                UpdateEconomyNotificationPreferencesRequest request,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    request.HouseholdId,
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr<EconomyNotificationPreferencesResponse>>(
                    new UpdateEconomyNotificationPreferencesCommand(
                        request.HouseholdId,
                        request.BudgetAlertsEnabled,
                        request.BillAlertsEnabled,
                        request.TrialAlertsEnabled),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("UpdateEconomyNotificationPreferences")
        .WithSummary("Update economy alert preferences for a household.")
        .Produces<EconomyNotificationPreferencesResponse>()
        .RequireAuthorization();
    }
}
