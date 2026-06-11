using FluentValidation;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Contracts;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.ConfirmEstimatedBill;

internal static class ConfirmEstimatedBillEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/recurring-bills/{{recurringBillId:guid}}/confirm",
            async (
                Guid recurringBillId,
                ConfirmEstimatedBillRequest request,
                IValidator<ConfirmEstimatedBillRequest> validator,
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
                    new ConfirmEstimatedBillCommand(
                        request.HouseholdId,
                        recurringBillId,
                        request.TransactionId,
                        request.Amount.Amount,
                        request.Amount.Currency,
                        request.OccurredOn),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("ConfirmEconomyEstimatedBill")
        .WithSummary("Confirm a pending estimated recurring bill with the actual amount.")
        .Produces<TransactionResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
