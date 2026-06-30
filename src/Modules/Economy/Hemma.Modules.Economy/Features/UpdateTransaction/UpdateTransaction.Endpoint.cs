using FluentValidation;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.UpdateTransaction;

internal static class UpdateTransactionEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut($"{EconomyRoutes.Prefix}/transactions/{{transactionId:guid}}",
            async (
                Guid transactionId,
                UpdateTransactionRequest request,
                IValidator<UpdateTransactionRequest> validator,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                var forbidden = await EconomyEndpointAuthorization.AuthorizeHouseholdAsync(
                    request.HouseholdId,
                    HouseholdsPermissions.HouseholdsWrite,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<TransactionResponse>>(
                    new UpdateTransactionCommand(
                        request.HouseholdId,
                        transactionId,
                        request.AccountId,
                        request.CategoryId,
                        request.Amount.Amount,
                        request.Amount.Currency,
                        request.OccurredOn,
                        request.Note,
                        request.Kind,
                        request.PayerId),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("UpdateEconomyTransaction")
        .WithSummary("Update an economy expense or income transaction.")
        .Produces<TransactionResponse>()
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
