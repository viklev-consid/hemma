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

namespace Hemma.Modules.Economy.Features.CreateRecurringBill;

internal static class CreateRecurringBillEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/recurring-bills",
            async (
                CreateRecurringBillRequest request,
                IValidator<CreateRecurringBillRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<RecurringBillResponse>>(
                    new CreateRecurringBillCommand(
                        request.HouseholdId,
                        request.Name,
                        request.AccountId,
                        request.CategoryId,
                        request.Amount.Amount,
                        request.Amount.Currency,
                        request.Type,
                        request.Direction,
                        request.CadenceFrequency,
                        request.CadenceInterval,
                        request.CadenceDayOfMonth,
                        request.StartsOn,
                        request.Note),
                    ct);

                return result.ToProblemDetailsOr(response => Results.Created($"{EconomyRoutes.Prefix}/recurring-bills/{response.RecurringBillId}", response));
            })
        .WithName("CreateEconomyRecurringBill")
        .WithSummary("Create a recurring fixed or estimated economy bill.")
        .Produces<RecurringBillResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
