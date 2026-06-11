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

namespace Hemma.Modules.Economy.Features.UpsertBudgetLine;

internal static class UpsertBudgetLineEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut($"{EconomyRoutes.Prefix}/budgets/lines",
            async (
                UpsertBudgetLineRequest request,
                IValidator<UpsertBudgetLineRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<BudgetResponse>>(
                    new UpsertBudgetLineCommand(
                        request.HouseholdId,
                        request.BudgetId,
                        request.CategoryId,
                        request.Amount.Amount,
                        request.Amount.Currency),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("UpsertEconomyBudgetLine")
        .WithSummary("Create or update a budget line.")
        .Produces<BudgetResponse>()
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
