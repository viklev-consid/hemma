using FluentValidation;
using Hemma.Shared.Contracts;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Economy.Features.CreateBudget;

internal static class CreateBudgetEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/budgets",
            async (
                CreateBudgetRequest request,
                IValidator<CreateBudgetRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CreateBudgetResult>>(
                    new CreateBudgetCommand(request.HouseholdId, request.AnchorDate),
                    ct);
                return result.ToProblemDetailsOr(response => response.Created
                    ? Results.Created($"{EconomyRoutes.Prefix}/budgets/{response.Budget.BudgetId}", response.Budget)
                    : Results.Ok(response.Budget));
            })
        .WithName("CreateEconomyBudget")
        .WithSummary("Create or return the budget for the cycle containing the anchor date.")
        .Produces<BudgetResponse>(StatusCodes.Status201Created)
        .Produces<BudgetResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
