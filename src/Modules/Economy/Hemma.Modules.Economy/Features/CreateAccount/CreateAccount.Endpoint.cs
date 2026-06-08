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

namespace Hemma.Modules.Economy.Features.CreateAccount;

internal static class CreateAccountEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/accounts",
            async (
                CreateAccountRequest request,
                IValidator<CreateAccountRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<AccountResponse>>(
                    new CreateAccountCommand(
                        request.HouseholdId,
                        request.Name,
                        request.Type,
                        request.OpeningBalance.Amount,
                        request.OpeningBalance.Currency),
                    ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{EconomyRoutes.Prefix}/accounts/{response.AccountId}", response));
            })
        .WithName("CreateEconomyAccount")
        .WithSummary("Create an economy account for a household.")
        .Produces<AccountResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
