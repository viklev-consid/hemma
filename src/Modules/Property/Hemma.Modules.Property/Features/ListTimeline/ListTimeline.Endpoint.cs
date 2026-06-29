using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Features.Shared;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Http;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace Hemma.Modules.Property.Features.ListTimeline;

internal static class ListTimelineEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet($"{PropertyRoutes.Prefix}/timeline",
                async (
                    Guid householdId,
                    int? year,
                    Guid? areaId,
                    string? type,
                    [FromQuery] Guid[]? tagIds,
                    int? offset,
                    int? limit,
                    IScopedAuthorizationService<HouseholdScope> authorization,
                    ICurrentUser currentUser,
                    IMessageBus bus,
                    CancellationToken ct) =>
                {
                    var forbidden = await PropertyEndpointAuthorization.AuthorizeHouseholdAsync(
                        householdId,
                        PropertyPermissions.Read,
                        authorization,
                        currentUser,
                        ct);
                    if (forbidden is not null)
                    {
                        return forbidden;
                    }

                    var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListTimelineResponse>>(
                        new ListTimelineQuery(householdId, year, areaId, type, tagIds, offset, limit),
                        ct);
                    return result.ToProblemDetailsOr(Results.Ok);
                })
            .WithName("ListPropertyTimeline")
            .WithTags(PropertyRoutes.GroupTag)
            .WithSummary("List property timeline items.")
            .Produces<ListTimelineResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();
    }
}
