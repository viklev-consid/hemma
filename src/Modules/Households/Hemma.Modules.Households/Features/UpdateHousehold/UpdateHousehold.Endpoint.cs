using FluentValidation;
using Hemma.Modules.Households.Authorization;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Households.Errors;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Households.Features.UpdateHousehold;

internal static class UpdateHouseholdEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch(HouseholdsRoutes.ByRef,
            async (
                string householdRef,
                UpdateHouseholdRequest request,
                IValidator<UpdateHouseholdRequest> validator,
                IHouseholdRefResolver resolver,
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

                var household = await resolver.ResolveAsync(householdRef, ct);
                if (household.IsError)
                {
                    return household.ToProblemDetailsOr(_ => Results.Empty);
                }

                var access = await authorization.AuthorizeAsync(
                    currentUser,
                    household.Value,
                    HouseholdsPermissions.HouseholdsWrite,
                    ScopedAuthorizationOptions.WithPlatformOverride,
                    ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }
                if (access.AccessMode == ScopedAuthorizationAccessMode.PlatformOverride)
                {
                    return Results.Problem(title: "Forbidden", detail: HouseholdsErrors.PlatformOverrideMutationForbidden.Description, statusCode: StatusCodes.Status403Forbidden);
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<UpdateHouseholdResponse>>(
                    new UpdateHouseholdCommand(household.Value.Id, request.Name, request.Slug),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("UpdateHousehold")
        .WithSummary("Update household settings.")
        .Produces<UpdateHouseholdResponse>()
        .RequireAuthorization();
}
