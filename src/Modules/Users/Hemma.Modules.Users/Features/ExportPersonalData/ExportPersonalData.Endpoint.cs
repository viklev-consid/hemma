using Hemma.Modules.Users.Domain;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Users.Features.ExportPersonalData;

internal static class ExportPersonalDataEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(UsersRoutes.PersonalData,
            async (ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var query = new ExportPersonalDataQuery(new UserId(userId));
                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ExportPersonalDataResponse>>(query, ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ExportPersonalData")
        .WithSummary("Export all personal data held about the authenticated user.")
        .Produces<ExportPersonalDataResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
}
