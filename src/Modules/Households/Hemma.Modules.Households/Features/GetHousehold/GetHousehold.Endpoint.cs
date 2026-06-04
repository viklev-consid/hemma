using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hemma.Modules.Households.Authorization;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Households.Features.GetHousehold;

internal static class GetHouseholdEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(HouseholdsRoutes.ByRef,
            async (
                string householdRef,
                IHouseholdRefResolver resolver,
                IScopedAuthorizationService<HouseholdScope> authorization,
                ICurrentUser currentUser,
                IMessageBus bus,
                CancellationToken ct) =>
            {
                var household = await resolver.ResolveAsync(householdRef, ct);
                if (household.IsError)
                {
                    return household.ToProblemDetailsOr(_ => Results.NotFound());
                }

                var access = await authorization.AuthorizeAsync(
                    currentUser,
                    household.Value,
                    HouseholdsPermissions.HouseholdsRead,
                    ScopedAuthorizationOptions.WithPlatformOverride,
                    ct);
                if (!access.Succeeded)
                {
                    return Results.Forbid();
                }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<GetHouseholdResponse>>(
                    new GetHouseholdQuery(household.Value.Id, access.AccessMode),
                    ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
        .WithName("GetHousehold")
        .WithSummary("Get an household by ID or slug.")
        .Produces<GetHouseholdResponse>()
        .RequireAuthorization();
}
