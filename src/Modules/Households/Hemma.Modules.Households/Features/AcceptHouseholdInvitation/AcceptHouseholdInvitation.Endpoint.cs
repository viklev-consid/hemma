using System.Security.Claims;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Households.Features.AcceptHouseholdInvitation;

internal static class AcceptHouseholdInvitationEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(HouseholdsRoutes.AcceptInvitation,
            async (
                AcceptHouseholdInvitationRequest request,
                HttpContext httpContext,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var email = httpContext.User.FindFirst("email")?.Value
                    ?? httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
                if (currentUser.Id is null || email is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<AcceptHouseholdInvitationResponse>>(
                    new AcceptHouseholdInvitationCommand(request.InvitationToken, userId, email),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("AcceptHouseholdInvitation")
        .WithSummary("Accept an household invitation for the current user.")
        .Produces<AcceptHouseholdInvitationResponse>()
        .RequireAuthorization();
}
