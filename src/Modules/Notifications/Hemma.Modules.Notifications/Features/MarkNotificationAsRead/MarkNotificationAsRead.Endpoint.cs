using ErrorOr;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Notifications.Features.MarkNotificationAsRead;

internal static class MarkNotificationAsReadEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch(NotificationsRoutes.MyNotificationRead,
            async (Guid notificationId, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await bus.InvokeAsync<ErrorOr<Success>>(
                    new MarkNotificationAsReadCommand(userId, notificationId),
                    ct);

                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
        .WithName("MarkNotificationAsRead")
        .WithSummary("Mark one in-app notification as read.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
}
