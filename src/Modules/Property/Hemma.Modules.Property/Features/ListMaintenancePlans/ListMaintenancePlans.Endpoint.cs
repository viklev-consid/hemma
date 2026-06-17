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

namespace Hemma.Modules.Property.Features.ListMaintenancePlans;

internal static class ListMaintenancePlansEndpoint
{
    private const string plansPrefix = $"{PropertyRoutes.Prefix}/maintenance/plans";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(plansPrefix,
                    async (
                        Guid householdId,
                        bool? activeOnly,
                        Guid? areaId,
                        [Microsoft.AspNetCore.Mvc.FromQuery] Guid[]? tagIds,
                        IScopedAuthorizationService<HouseholdScope> authorization,
                        ICurrentUser currentUser,
                        IMessageBus bus,
                        CancellationToken ct) =>
                    {
                        var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListMaintenancePlansResponse>>(
                            new ListMaintenancePlansQuery(householdId, activeOnly, areaId, tagIds),
                            ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("ListPropertyMaintenancePlans")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<ListMaintenancePlansResponse>()
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
