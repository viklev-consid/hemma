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

namespace Hemma.Modules.Property.Features.ListPropertyActivity;

internal static class ListPropertyActivityEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet($"{PropertyRoutes.Prefix}/activity",
                async (
                    Guid householdId,
                    DateTimeOffset? since,
                    string? targetType,
                    Guid? targetId,
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

                    var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListPropertyActivityResponse>>(
                        new ListPropertyActivityQuery(householdId, since, targetType, targetId, limit),
                        ct);
                    return result.ToProblemDetailsOr(Results.Ok);
                })
            .WithName("ListPropertyActivity")
            .WithTags(PropertyRoutes.GroupTag)
            .WithSummary("List recent property activity.")
            .Produces<ListPropertyActivityResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();
    }
}
