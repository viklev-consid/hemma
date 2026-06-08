using FluentValidation;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.RecordTransaction;

internal static class RecordTransactionEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/transactions",
            async (
                RecordTransactionRequest request,
                IValidator<RecordTransactionRequest> validator,
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
                    HouseholdsPermissions.HouseholdsRead,
                    authorization,
                    currentUser,
                    ct);
                if (forbidden is not null)
                {
                    return forbidden;
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<TransactionResponse>>(
                    new RecordTransactionCommand(
                        request.HouseholdId,
                        request.AccountId,
                        request.CategoryId,
                        request.Amount.Amount,
                        request.Amount.Currency,
                        request.OccurredOn,
                        request.Note,
                        request.Kind,
                        request.PayerId),
                    ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{EconomyRoutes.Prefix}/transactions/{response.TransactionId}", response));
            })
        .WithName("RecordEconomyTransaction")
        .WithSummary("Record an economy expense or income transaction.")
        .Produces<TransactionResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
