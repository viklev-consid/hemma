using ErrorOr;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Notifications.Features.GetMyNotificationPreferences;

internal static class GetMyNotificationPreferencesEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(NotificationsRoutes.MyNotificationPreferences,
            async (ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await bus.InvokeAsync<ErrorOr<GetMyNotificationPreferencesResponse>>(
                    new GetMyNotificationPreferencesQuery(userId),
                    ct);

                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetMyNotificationPreferences")
        .WithSummary("Get the authenticated user's notification preferences.")
        .Produces<GetMyNotificationPreferencesResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
}
