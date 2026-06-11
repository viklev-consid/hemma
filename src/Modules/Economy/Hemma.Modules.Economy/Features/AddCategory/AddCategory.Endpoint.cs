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

namespace Hemma.Modules.Economy.Features.AddCategory;

internal static class AddCategoryEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost($"{EconomyRoutes.Prefix}/categories",
            async (
                AddCategoryRequest request,
                IValidator<AddCategoryRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CategoryResponse>>(
                    new AddCategoryCommand(request.HouseholdId, request.Name, request.ParentCategoryId, request.Budgetable),
                    ct);
                return result.ToProblemDetailsOr(response => Results.Created($"{EconomyRoutes.Prefix}/categories/{response.CategoryId}", response));
            })
        .WithName("AddEconomyCategory")
        .WithSummary("Add an economy category for a household.")
        .Produces<CategoryResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
