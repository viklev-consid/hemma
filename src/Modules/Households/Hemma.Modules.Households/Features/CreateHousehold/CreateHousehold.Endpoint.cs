using FluentValidation;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Households.Features.CreateHousehold;

internal static class CreateHouseholdEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(HouseholdsRoutes.Prefix,
            async (
                CreateHouseholdRequest request,
                IValidator<CreateHouseholdRequest> validator,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                var command = new CreateHouseholdCommand(request.Name, request.Slug, userId);
                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<CreateHouseholdResponse>>(command, ct);
                return result.ToProblemDetailsOr(r => Results.Created($"{HouseholdsRoutes.Prefix}/{r.Slug}", r));
            })
        .WithName("CreateHousehold")
        .WithSummary("Create an household and make the caller its owner.")
        .Produces<CreateHouseholdResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
