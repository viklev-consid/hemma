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

namespace Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;

internal static class CopyBudgetFromPreviousPeriodEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/budgets/copy-from-previous",
            async (
                CopyBudgetFromPreviousPeriodRequest request,
                IValidator<CopyBudgetFromPreviousPeriodRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<BudgetResponse>>(
                    new CopyBudgetFromPreviousPeriodCommand(request.HouseholdId, request.AnchorDate),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("CopyEconomyBudgetFromPreviousPeriod")
        .WithSummary("Copy budget lines from the previous cycle period.")
        .Produces<BudgetResponse>()
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
