using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.DeleteTransaction;

internal static class DeleteTransactionEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete($"{EconomyRoutes.Prefix}/transactions/{{transactionId:guid}}",
            async (
                Guid transactionId,
                Guid householdId,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (householdId == Guid.Empty)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>(StringComparer.Ordinal) { ["householdId"] = ["Household id is required."] },
                        statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    householdId,
                    HouseholdsPermissions.HouseholdsWrite,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Success>>(
                    new DeleteTransactionCommand(householdId, transactionId),
                    ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
        .WithName("DeleteEconomyTransaction")
        .WithSummary("Delete an economy expense or income transaction.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
