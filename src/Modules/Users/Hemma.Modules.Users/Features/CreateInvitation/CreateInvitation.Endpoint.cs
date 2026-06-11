using FluentValidation;
using Hemma.Modules.Users.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Users.Features.CreateInvitation;

internal static class CreateInvitationEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(UsersRoutes.Invitations,
            async (
                CreateInvitationRequest request,
                [Microsoft.AspNetCore.Mvc.FromServices] IValidator<CreateInvitationRequest> validator,
                ICurrentUser currentUser,
                HttpContext http,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                if (!Guid.TryParse(currentUser.Id, out var invitedByUserId))
                {
                    return Results.Unauthorized();
                }

                var ip = http.Connection.RemoteIpAddress?.ToString();
                var ua = http.Request.Headers.UserAgent.ToString();
                var command = new CreateInvitationCommand(request.Email, invitedByUserId, ip, ua);
                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CreateInvitationResponse>>(command, ct);
                return result.ToProblemDetailsOr(r => Results.Created($"{UsersRoutes.Invitations}/{r.InvitationId}", r));
            })
        .WithName("CreateInvitation")
        .WithSummary("Create a user invitation. Admin only.")
        .Produces<CreateInvitationResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(UsersPermissions.InvitationsWrite)
        .RequireRateLimiting("write");
}
