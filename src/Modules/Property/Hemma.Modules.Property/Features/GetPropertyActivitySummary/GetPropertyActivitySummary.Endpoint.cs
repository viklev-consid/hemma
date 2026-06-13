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

namespace Hemma.Modules.Property.Features.GetPropertyActivitySummary;

internal static class GetPropertyActivitySummaryEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet($"{PropertyRoutes.Prefix}/activity/summary",
                async (
                    Guid householdId,
                    DateTimeOffset? since,
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

                    var result = await bus.InvokeAsync<ErrorOr.ErrorOr<PropertyActivitySummaryResponse>>(
                        new GetPropertyActivitySummaryQuery(householdId, since),
                        ct);
                    return result.ToProblemDetailsOr(Results.Ok);
                })
            .WithName("GetPropertyActivitySummary")
            .WithTags(PropertyRoutes.GroupTag)
            .WithSummary("Get property activity counts.")
            .Produces<PropertyActivitySummaryResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization();
    }
}
