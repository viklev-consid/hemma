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

namespace Hemma.Modules.Property.Features.ListProjects;

internal static class ListProjectsEndpoint
{

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet($"{PropertyRoutes.Prefix}/projects",
                    async (
                        Guid householdId,
                        string? status,
                        Guid? areaId,
                        string? priority,
                        [FromQuery] Guid[]? tagIds,
                        bool? isOverdue,
                        IScopedAuthorizationService<HouseholdScope> authorization,
                        ICurrentUser currentUser,
                        IMessageBus bus,
                        CancellationToken ct) =>
                    {
                        var forbidden = await AuthorizeAsync(householdId, PropertyPermissions.Read, authorization, currentUser, ct);
                        if (forbidden is not null) { return forbidden; }

                        var result = await bus.InvokeAsync<ErrorOr.ErrorOr<ListProjectsResponse>>(
                            new ListProjectsQuery(householdId, status, areaId, priority, tagIds, isOverdue),
                            ct);
                        return result.ToProblemDetailsOr(Results.Ok);
                    })
                    .WithName("ListPropertyProjects")
                    .WithTags(PropertyRoutes.GroupTag)
                    .Produces<ListProjectsResponse>()
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
