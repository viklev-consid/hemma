using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Features.Shared;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property.Features.DeactivatePlan;

internal static class DeactivatePlanEndpoint
{
    private const string plansPrefix = $"{PropertyRoutes.Prefix}/maintenance/plans";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost($"{plansPrefix}/{{planId:guid}}/deactivate",
                    async (
                        Guid planId,
                        Guid householdId,
                        IScopedAuthorizationService<HouseholdScope> authorization,
                        ICurrentUser currentUser,
                        IMessageBus bus,
                        CancellationToken ct) =>
                    {
                        var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Write, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<MaintenancePlanResponse>>(new DeactivatePlanCommand(planId, householdId), ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("DeactivatePropertyMaintenancePlan")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<MaintenancePlanResponse>()
                    .RequireAuthorization();
    }

    private static Task<IResult?> AuthorizeAsync(
        Guid householdId,
        string permission,
        IScopedAuthorizationService<HouseholdScope> authorization,
        ICurrentUser currentUser,
        CancellationToken ct) =>
        PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, permission, authorization, currentUser, ct);
}
