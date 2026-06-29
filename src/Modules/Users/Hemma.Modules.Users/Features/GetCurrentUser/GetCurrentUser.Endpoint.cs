using Hemma.Modules.Users.Domain;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Users.Features.GetCurrentUser;

internal static class GetCurrentUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(UsersRoutes.Me,
            async (ICurrentUser currentUser, IMessageBus bus, HttpContext http, CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                // User-specific data; must not be stored in shared caches.
                http.Response.Headers.CacheControl = "private, no-store";

                var query = new GetCurrentUserQuery(new UserId(userId), currentUser.Role);
                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetCurrentUserResponse>>(query, ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetCurrentUser")
        .WithSummary("Get the authenticated user's profile, role, and resolved permissions.")
        .Produces<GetCurrentUserResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
}
