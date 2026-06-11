using Hemma.Modules.Users.Contracts.Authorization;
using Hemma.Modules.Users.Domain;
using Hemma.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Users.Features.GetUserById;

internal static class GetUserByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(UsersRoutes.ById,
            async (Guid userId, IMessageBus bus, CancellationToken ct) =>
            {
                var query = new GetUserByIdQuery(new UserId(userId));
                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetUserByIdResponse>>(query, ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetUserById")
        .WithSummary("Get a specific user by ID. Requires users.users.read permission.")
        .Produces<GetUserByIdResponse>()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(UsersPermissions.UsersRead)
        .RequireRateLimiting("read");
}
