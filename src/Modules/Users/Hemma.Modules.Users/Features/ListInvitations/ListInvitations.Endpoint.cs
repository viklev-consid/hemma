using Hemma.Modules.Users.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Users.Features.ListInvitations;

internal static class ListInvitationsEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(UsersRoutes.Invitations,
            async (IMessageBus bus, CancellationToken ct, int page = 1, int pageSize = 20, string? status = "pending") =>
            {
                var query = new ListInvitationsQuery(page, pageSize, status);
                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListInvitationsResponse>>(query, ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ListInvitations")
        .WithSummary("List user invitations. Requires users.invitations.write permission.")
        .Produces<ListInvitationsResponse>()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(UsersPermissions.InvitationsWrite)
        .RequireRateLimiting("read");
}
