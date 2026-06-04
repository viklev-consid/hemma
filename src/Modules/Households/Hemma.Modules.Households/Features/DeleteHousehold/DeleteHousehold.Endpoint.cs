using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Modules.Households.Authorization;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Households.Errors;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Households.Features.DeleteHousehold;

internal static class DeleteHouseholdEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(HouseholdsRoutes.ByRef,
            async (
                string householdRef,
                IHouseholdRefResolver resolver,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                if (currentUser.Id is null || !Guid.TryParse(currentUser.Id, out var userId))
                {
                    return Results.Unauthorized();
                }

                var household = await resolver.ResolveAsync(householdRef, ct);
                if (household.IsError)
                {
                    return household.ToProblemDetailsOr(_ => Results.Empty);
                }

                var access = await authorization.AuthorizeAsync(
                    currentUser,
                    household.Value,
                    HouseholdsPermissions.HouseholdsDelete,
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

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ErrorOr.Success>>(
                    new DeleteHouseholdCommand(household.Value.Id, userId),
                    ct);
                return result.ToProblemDetailsOr(_ => Results.NoContent());
            })
        .WithName("DeleteHousehold")
        .WithSummary("Soft-delete an household.")
        .Produces(StatusCodes.Status204NoContent)
        .RequireAuthorization();
}
