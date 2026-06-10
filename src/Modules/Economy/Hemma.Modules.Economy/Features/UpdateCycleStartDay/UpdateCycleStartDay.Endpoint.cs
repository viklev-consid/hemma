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

namespace Hemma.Modules.Economy.Features.UpdateCycleStartDay;

internal static class UpdateCycleStartDayEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut($"{EconomyRoutes.Prefix}/settings/cycle-start-day",
            async (
                UpdateCycleStartDayRequest request,
                IValidator<UpdateCycleStartDayRequest> validator,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<UpdateCycleStartDayResponse>>(
                    new UpdateCycleStartDayCommand(request.HouseholdId, request.CycleStartDay),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("UpdateEconomyCycleStartDay")
        .WithSummary("Update the economy cycle start day for a household.")
        .Produces<UpdateCycleStartDayResponse>()
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
}
