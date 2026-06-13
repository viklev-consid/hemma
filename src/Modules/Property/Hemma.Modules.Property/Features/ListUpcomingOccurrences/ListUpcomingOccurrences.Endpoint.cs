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

namespace Hemma.Modules.Property.Features.ListUpcomingOccurrences;

internal static class ListUpcomingOccurrencesEndpoint
{
    private const string occurrencesPrefix = $"{PropertyRoutes.Prefix}/maintenance/occurrences";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(occurrencesPrefix,
            async (Guid householdId, int? horizonDays, bool? isOverdue, IScopedAuthorizationService<HouseholdScope> authorization, ICurrentUser currentUser, IMessageBus bus, CancellationToken ct) =>
            {
                var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                if (forbidden is not null) { return forbidden; }

                var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListUpcomingOccurrencesResponse>>(new ListUpcomingOccurrencesQuery(householdId, horizonDays.GetValueOrDefault(30), isOverdue), ct);
                return result.ToProblemDetailsOr(Results.Ok);
            })
            .WithName("ListPropertyUpcomingMaintenanceOccurrences")
            .WithTags(PropertyRoutes.GroupTag)
            .Produces<ListUpcomingOccurrencesResponse>()
            .RequireAuthorization();
}
