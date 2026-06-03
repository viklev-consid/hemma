using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Users.Features.LogoutAll;

internal static class LogoutAllEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(UsersRoutes.LogoutAll,
            async (
                [Microsoft.AspNetCore.Mvc.FromServices] ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (!Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var command = new LogoutAllCommand(userId);
                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<LogoutAllResponse>>(command, ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("LogoutAll")
        .WithSummary("Revoke all refresh tokens for the authenticated user (logout everywhere).")
        .Produces<LogoutAllResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization()
        .RequireRateLimiting("write");
}
